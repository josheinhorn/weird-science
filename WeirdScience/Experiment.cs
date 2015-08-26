﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WeirdScience
{
    // Consider state machine or state pattern? State pattern would make it harder to extend right?
    // Experiment is the primary point of Extensibility

    // TODO: Should there be an experiment with no Generic e.g. returns Void? What would that even do?

    public class Experiment<T, TPublish> : IScienceExperiment<T, TPublish>
    {
        #region Private Fields

        private const string ControlName = "WeirdScience.Control";

        private IExperimentState<TPublish> _currentState;

        private string _name;

        private ISciencePublisher _publisher;

        private IExperimentSteps<T, TPublish> _steps;

        private bool _throwOnInternalExceptions;

        //possibly allow this to be injected?
        private ExceptionEqualityComparer exceptionComparer = new ExceptionEqualityComparer();

        #endregion Private Fields

        #region Public Constructors

        public Experiment(string name, ISciencePublisher publisher, IExperimentState<TPublish> state,
            IExperimentSteps<T, TPublish> steps, bool throwOnInternalExceptions)
        {
            _name = name;
            _publisher = publisher; //if this is null, nothing gets published! should we throw?
            _throwOnInternalExceptions = throwOnInternalExceptions;
            _currentState = state;
            _steps = steps; //should be overwriten by public Set
        }

        public Experiment(string name, ISciencePublisher publisher, IExperimentState<TPublish> state,
            bool throwOnInternalExceptions)
            : this(name, publisher, state, new ExperimentSteps<T, TPublish>(), throwOnInternalExceptions)
        { }

        public Experiment(string name, ISciencePublisher publisher, bool throwOnInternalExceptions)
            : this(name, publisher, new ExperimentState<TPublish>(), throwOnInternalExceptions)
        { }

        public Experiment(string name, ISciencePublisher publisher)
            : this(name, publisher, new ExperimentState<TPublish>(), false)
        { }

        #endregion Public Constructors

        #region Public Properties

        public virtual string Name { get { return _name; } }

        public virtual IExperimentSteps<T, TPublish> Steps { get { return _steps; } set { _steps = value; } }

        #endregion Public Properties

        #region Protected Properties

        protected virtual IExperimentState<TPublish> CurrentState { get { return _currentState; } }
        protected virtual ISciencePublisher Publisher { get { return _publisher; } }
        protected virtual bool ThrowOnInternalExceptions { get { return _throwOnInternalExceptions; } }

        #endregion Protected Properties

        #region Public Methods

        public virtual bool AreEqual(T control, T candidate)
        {
            CurrentState.CurrentStep = Operations.AreEqual;
            return TryStep(() =>
            {
                return Steps.AreEqual == null ?
                EqualityComparer<T>.Default.Equals(control, candidate)
                : Steps.AreEqual(control, candidate);
            });
        }

        public virtual object Context()
        {
            CurrentState.CurrentStep = Operations.SetContext;
            return TryStep(() =>
            {
                return Steps.SetContext != null ? Steps.SetContext() : null;
            });
        }

        public virtual bool Ignore(T control, T candidate)
        {
            CurrentState.CurrentStep = Operations.Ignore;
            return TryStep(() =>
            {
                return Steps.Ignore == null ? false : Steps.Ignore(control, candidate);
            });
        }

        public virtual string OnError(IExperimentError error)
        {
            CurrentState.CurrentStep = Operations.OnError;
            if (error == null) throw new ArgumentNullException("error");
            return TryStep(() =>
            {
                if (Steps.OnError != null) return Steps.OnError(error);
                return string.Empty;
            });
        }

        public virtual string OnMismatch(T control, T candidate,
            Exception controlException, Exception candidateException)
        {
            CurrentState.CurrentStep = Operations.OnMismatch;
            return TryStep(() =>
            {
                if (Steps.OnMismatch != null)
                    return Steps.OnMismatch(control, candidate, controlException, candidateException);
                return string.Empty;
            });
        }

        public virtual bool PreCondition()
        {
            CurrentState.CurrentStep = Operations.PreCondition;
            return TryStep(() =>
            {
                return Steps.PreCondition == null ? true : Steps.PreCondition();
            });
        }

        public virtual TPublish Prepare(T result)
        {
            CurrentState.CurrentStep = Operations.Prepare;
            return TryStep(() =>
            {
                if (Steps.Prepare == null)
                {
                    if (typeof(TPublish).IsAssignableFrom(typeof(T)))
                    {
                        return (TPublish)(object)result;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            string.Format("No Prepare method is available and cannot cast Type '{0}'"
                            + " to Type '{1}'.", typeof(T).FullName, typeof(TPublish).FullName));
                    }
                }
                else return Steps.Prepare(result);
            });
        }

        public virtual void Publish(IExperimentResult<TPublish> results)
        {
            CurrentState.CurrentStep = Operations.Publish;
            TryStep(() =>
            {
                if (Publisher != null)
                    Publisher.Publish(results);
                return 0;
            });
        }

        public virtual void Publish(string message, IExperimentState<TPublish> state)
        {
            CurrentState.CurrentStep = Operations.Publish;
            TryStep(() =>
            {
                if (Publisher != null)
                    Publisher.Publish(message, state);
                return 0;
            });
        }

        public T Run() //Should this be virtual? overriding this breaks the class' logic, so no
        {
            if (CurrentState == null)
            {
                throw new InvalidOperationException(
                    "The Experiment State is null! This Experiment has likely already been complete. " +
                    "Can't run Experiment.");
            }
            IExperimentResult<TPublish> results = new ExperimentResult<TPublish>
            {
                LastState = CurrentState,
                Name = Name
            };
            T controlValue = default(T);
            Exception controlException = null;
            try
            {
                controlValue = RunControl(results, out controlException); //If this throws, the control itself failed
            }
            catch (Exception)
            {
                //Fatal error occurred!
                if (ThrowOnInternalExceptions) throw;
                //continue on with the candidates
            }
            try
            {
                RunCandidates(results, controlValue, controlException);
                Publish(results);
            }
            catch (Exception)
            {
                //A fatal error occured in one of the candidates or Publisher, throw for debug purposes if flag is set
                if (ThrowOnInternalExceptions) throw;
            }
            _currentState = null;
            if (controlException != null)
                throw controlException;
            else
                return controlValue;
        }

        public virtual bool RunInParallel()
        {
            CurrentState.CurrentStep = Operations.RunInParallel;
            return TryStep(() =>
            {
                return Steps.RunInParallel == null ? false : Steps.RunInParallel();
            });
        }

        public virtual string Setup()
        {
            CurrentState.CurrentStep = Operations.Setup;
            return TryStep(() =>
            {
                if (Steps.Setup != null) return Steps.Setup();
                return string.Empty;
            });
        }

        public virtual string Teardown()
        {
            CurrentState.CurrentStep = Operations.Teardown;
            return TryStep(() =>
            {
                if (Steps.Teardown != null) return Steps.Teardown();
                return string.Empty;
            });
        }

        public virtual long Timeout()
        {
            CurrentState.CurrentStep = Operations.SetTimeout;
            return TryStep(() =>
            {
                return Steps.SetTimeout == null ? 0 : Steps.SetTimeout();
            });
        }

        #endregion Public Methods

        #region Private Methods

        private void RunCandidates(IExperimentResult<TPublish> results, T controlResult, Exception controlException)
        {
            var candidates = Steps.GetCandidates();
            if (candidates != null)
            {
                var timer = new Stopwatch();
                foreach (var candidate in candidates)
                {
                    T candResult = default(T);
                    TPublish candValue = default(TPublish);
                    bool mismatched = false;
                    try
                    {
                        //Should this be a State machine?
                        CurrentState.Name = candidate.Key;
                        if (PreCondition() && candidate.Value != null)
                        {
                            var context = Context();
                            Publish(Setup(), CurrentState.Snapshot());
                            timer.Restart();
                            candResult = TryCandidate(candidate.Value);
                            timer.Stop();
                            if (!Ignore(controlResult, candResult) &&
                                (controlException != null || !AreEqual(controlResult, candResult)))
                            {
                                mismatched = true;
                                var misMessage = OnMismatch(controlResult, candResult, controlException, null);
                                Publish(misMessage, CurrentState.Snapshot());
                            }
                            candValue = Prepare(candResult);
                            Publish(Teardown(), CurrentState.Snapshot());
                            if (!results.Candidates.ContainsKey(candidate.Key))
                            {
                                results.Candidates.Add(candidate.Key,
                                    new Observation<TPublish>
                                    {
                                        Value = candValue,
                                        ElapsedMilliseconds = timer.ElapsedMilliseconds,
                                        Context = context,
                                        IsMismatched = mismatched,
                                        ExceptionThrown = false,
                                        Name = candidate.Key
                                    });
                            }
                            else
                            {
                                //Duplicate keys... should not be possible
                            }
                        }
                    }
                    catch (StepFailedException sfe)
                    {
                        timer.Stop();
                        //it's possible for this to throw, which would result in an internal exception
                        Publish(OnError(sfe.ExperimentError), CurrentState.Snapshot());
                        if (!exceptionComparer.Equals(sfe.ExperimentError.LastException, controlException))
                        {
                            //It's a mismatch if the exceptions don't match
                            mismatched = true;
                            var misMessage = OnMismatch(controlResult, candResult,
                                controlException, sfe.ExperimentError.LastException); //potential throw
                            Publish(misMessage, CurrentState.Snapshot());
                        }
                        var errResult = new Observation<TPublish>
                        {
                            ExperimentError = sfe.ExperimentError,
                            Value = candValue,
                            ElapsedMilliseconds = timer.ElapsedMilliseconds,
                            ExceptionThrown = true,
                            Name = candidate.Key,
                            IsMismatched = mismatched
                        };
                        if (results.Candidates.ContainsKey(results.LastState.Name))
                            results.Candidates[results.LastState.Name] = errResult;
                        else
                            results.Candidates.Add(results.LastState.Name, errResult);
                    }
                }
            }
        }

        private T RunControl(IExperimentResult<TPublish> results, out Exception controlException)
        {
            controlException = null;
            CurrentState.Name = ControlName;
            CurrentState.CurrentStep = Operations.Control;
            if (Steps.Control == null)
            {
                throw new InvalidOperationException(
                    "The Control was never set for this Experiment! Can't run an Experiment without a Control!");
            }
            object context = null;
            T result = default(T);
            TPublish value = default(TPublish);
            try
            {
                context = Context();
            }
            catch (StepFailedException sfe)
            {
                Publish(OnError(sfe.ExperimentError), CurrentState.Snapshot());
                //We'll still run the Control even without a context
            }
            var timer = new Stopwatch();
            CurrentState.CurrentStep = Operations.Control; try
            {
                timer.Start();
                result = Steps.Control();
                timer.Stop();
            }
            catch (Exception e)
            {
                timer.Stop();
                controlException = e; //need to throw this at the end
                var error = new ExperimentError
                {
                    LastException = e,
                    LastStep = Operations.Control,
                    ErrorMessage = "An Exception was thrown running the Control! Exception message: "
                        + e.Message,
                    ExperimentName = ControlName
                };
                Publish(OnError(error), CurrentState.Snapshot());//TODO: Should we run OnError on Control??
                results.Control = new Observation<TPublish>
                {
                    ExperimentError = error,
                    ElapsedMilliseconds = timer.ElapsedMilliseconds,
                    Context = context,
                    Name = ControlName,
                    IsMismatched = false,
                    ExceptionThrown = true
                };
                return result;
            }
            bool excpThrown = false;
            try
            {
                value = Prepare(result);
            }
            catch (StepFailedException sfe)
            {
                excpThrown = true;
                Publish(OnError(sfe.ExperimentError), CurrentState.Snapshot());
                //We must continue on because we've already gotten a result
            }
            results.Control = new Observation<TPublish>
            {
                ElapsedMilliseconds = timer.ElapsedMilliseconds,
                Context = context,
                Name = ControlName,
                IsMismatched = false, //Control can't be mismatched
                ExceptionThrown = excpThrown,
                Value = value
            };
            return result;
        }

        private T TryCandidate(Func<T> candidate)
        {
            CurrentState.CurrentStep = Operations.Candidate;
            return TryStep(candidate);
        }

        private TOut TryStep<TOut>(Func<TOut> tryOp)
        {
            TOut result = default(TOut);
            try
            {
                result = tryOp();
            }
            catch (Exception e)
            {
                var error = new ExperimentError
                {
                    LastException = e,
                    LastStep = CurrentState.CurrentStep,
                    ErrorMessage = string.Format(
                        "An Exception was thrown running Step '{0}'. Exception message: {1}",
                        CurrentState.CurrentStep, e.Message),
                    ExperimentName = CurrentState.Name
                };
                throw new StepFailedException(error);
            }
            return result;
        }

        #endregion Private Methods
    }

    public class SimpleExperiment<T> : Experiment<T, T>
    {
        #region Public Constructors

        public SimpleExperiment(string name, ISciencePublisher publisher, bool raiseInternalExceptions)
            : base(name, publisher, raiseInternalExceptions)
        { }

        public SimpleExperiment(string name, ISciencePublisher publisher)
            : base(name, publisher, false)
        { }

        #endregion Public Constructors
    }
}