using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WeirdScience
{
    // Consider state machine or state pattern? State pattern would make it harder to extend right?
    // Experiment is the primary point of Extensibility

    // TODO: Should there be an experiment with no Generic e.g. returns Void? What would that even do?

    internal class SimpleExperiment<T> : Experiment<T, T>
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

    public class Experiment<T, TPublish> : IScienceExperiment<T, TPublish>
    {
        #region Private Fields

        private const string ControlName = "WeirdScience.Control";

        private bool throwOnInternalExceptions;

        //possibly allow this to be injected?
        private ExceptionEqualityComparer exceptionComparer = new ExceptionEqualityComparer();
        #endregion Private Fields

        #region Public Constructors

        public Experiment(string name, ISciencePublisher publisher, bool throwOnInternalExceptions)
        {
            Name = name;
            Publisher = publisher; //if this is null, nothing gets published! should we throw?
            this.throwOnInternalExceptions = throwOnInternalExceptions;
            CurrentState = new ExperimentState<TPublish>();
            Steps = new ExperimentSteps<T, TPublish>();
        }

        public Experiment(string name, ISciencePublisher publisher)
            : this(name, publisher, false)
        { }

        #endregion Public Constructors

        #region Public Properties

        public virtual string Name { get; private set; }

        public virtual IExperimentSteps<T, TPublish> Steps { get; protected set; }

        #endregion Public Properties

        #region Protected Properties

        protected virtual IExperimentState<TPublish> CurrentState { get; private set; }
        protected ISciencePublisher Publisher { get; set; }

        #endregion Protected Properties

        #region Public Methods

        public virtual bool AreEqual(T control, T candidate)
        {
            CurrentState.Step = Operations.AreEqual;
            CurrentState.Timestamp = DateTime.UtcNow;
            return TryStep(() =>
            {
                return Steps.AreEqual == null ?
                EqualityComparer<T>.Default.Equals(control, candidate)
                : Steps.AreEqual(control, candidate);
            });
        }

        public virtual object Context()
        {
            CurrentState.Step = Operations.Context;
            CurrentState.Timestamp = DateTime.UtcNow;
            return TryStep(() =>
            {
                return Steps.SetContext != null ? Steps.SetContext() : null;
            });
        }

        public virtual ICandidateBuilder<T, TPublish> Control(Func<T> control)
        {
            this.Steps.Control = control;
            return new CandidateBuilder<T, TPublish>(this);
        }

        public virtual bool Ignore(T control, T candidate)
        {
            CurrentState.Step = Operations.Ignore;
            CurrentState.Timestamp = DateTime.UtcNow;
            return TryStep(() =>
            {
                return Steps.Ignore == null ? false : Steps.Ignore(control, candidate);
            });
        }

        public virtual string OnError(IExperimentError error)
        {
            CurrentState.Step = Operations.OnError;
            CurrentState.Timestamp = DateTime.UtcNow;
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
            CurrentState.Step = Operations.OnMismatch;
            CurrentState.Timestamp = DateTime.UtcNow;
            return TryStep(() =>
            {
                if (Steps.OnMismatch != null)
                    return Steps.OnMismatch(control, candidate, controlException, candidateException);
                return string.Empty;
            });
        }

        public virtual bool PreCondition()
        {
            CurrentState.Step = Operations.PreCondition;
            CurrentState.Timestamp = DateTime.UtcNow;
            return TryStep(() =>
            {
                return Steps.PreCondition == null ? true : Steps.PreCondition();
            });
        }

        public virtual TPublish Prepare(T result)
        {
            CurrentState.Step = Operations.Prepare;
            CurrentState.Timestamp = DateTime.UtcNow;
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
            CurrentState.Step = Operations.Publish;
            CurrentState.Timestamp = DateTime.UtcNow;
            TryStep(() =>
            {
                if (Publisher != null)
                    Publisher.Publish(results);
                return 0;
            });
        }

        public virtual void Publish(string message, IExperimentState<TPublish> state)
        {
            CurrentState.Step = Operations.Publish;
            CurrentState.Timestamp = DateTime.UtcNow;
            TryStep(() =>
            {
                if (Publisher != null)
                    Publisher.Publish(message, state);
                return 0;
            });
        }

        public virtual T Run()
        {
            if (CurrentState == null)
            {
                throw new InvalidOperationException(
                    "The Experiment State is null! This Experiment has likely already been complete. " +
                    "Can't run Experiment.");
            }
            //if (Steps == null)
            //{
            //    throw new InvalidOperationException(
            //       "The Experiment Steps for this Experiment were never set. Can't run Experiment.");
            //}
            IExperimentResult<TPublish> results = new ExperimentResult<TPublish>
            {
                CurrentState = CurrentState,
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
                if (throwOnInternalExceptions) throw;
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
                if (throwOnInternalExceptions) throw;
            }
            CurrentState = null;
            if (controlException != null)
                throw controlException;
            else
                return controlValue;
        }

        public virtual bool RunInParallel()
        {
            CurrentState.Step = Operations.RunInParallel;
            CurrentState.Timestamp = DateTime.UtcNow;
            return TryStep(() =>
            {
                return Steps.RunInParallel == null ? false : Steps.RunInParallel();
            });
        }

        public virtual string Setup()
        {
            CurrentState.Step = Operations.Setup;
            CurrentState.Timestamp = DateTime.UtcNow;
            return TryStep(() =>
            {
                if (Steps.Setup != null) return Steps.Setup();
                return string.Empty;
            });
        }

        public virtual string Teardown()
        {
            CurrentState.Step = Operations.Teardown;
            CurrentState.Timestamp = DateTime.UtcNow;
            return TryStep(() =>
            {
                if (Steps.Teardown != null) return Steps.Teardown();
                return string.Empty;
            });
        }

        public virtual long Timeout()
        {
            CurrentState.Step = Operations.Timeout;
            CurrentState.Timestamp = DateTime.UtcNow;
            return TryStep(() =>
            {
                return Steps.SetTimeout == null ? 0 : Steps.SetTimeout();
            });
        }

        #endregion Public Methods

        #region Private Methods

        private T TryCandidate(Func<T> candidate)
        {
            CurrentState.Step = Operations.Candidate;
            CurrentState.Timestamp = DateTime.UtcNow;
            return TryStep(candidate);
        }
        private void RunCandidates(IExperimentResult<TPublish> results, T controlResult, Exception controlException)
        {
            if (Steps.Candidates != null)
            {
                var timer = new Stopwatch();
                foreach (var candidate in Steps.Candidates)
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
                            Publish(Setup(), CurrentState.GetSnapshot());
                            timer.Reset();
                            timer.Start();
                            candResult = TryCandidate(candidate.Value);
                            timer.Stop();
                            if (!Ignore(controlResult, candResult)
                                && controlException == null
                                && !AreEqual(controlResult, candResult))
                            {
                                mismatched = true;
                                var misMessage = OnMismatch(controlResult, candResult, null, null);
                                Publish(misMessage, CurrentState.GetSnapshot());
                            }
                            candValue = Prepare(candResult);
                            Publish(Teardown(), CurrentState.GetSnapshot());
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
                        Publish(OnError(sfe.ExperimentError), CurrentState.GetSnapshot());

                        if (!exceptionComparer.Equals(sfe.ExperimentError.LastException, controlException))
                        {
                            //It's a mismatch if the exceptions don't match
                            mismatched = true;
                            var misMessage = OnMismatch(controlResult, candResult,
                                controlException, sfe.ExperimentError.LastException); //potential throw
                            Publish(misMessage, CurrentState.GetSnapshot());
                        }
                        var errResult = new Observation<TPublish>
                        {
                            Error = sfe.ExperimentError,
                            Value = candValue,
                            ElapsedMilliseconds = timer.ElapsedMilliseconds,
                            ExceptionThrown = true,
                            Name = candidate.Key,
                            IsMismatched = mismatched
                        };
                        if (results.Candidates.ContainsKey(results.CurrentState.Name))
                            results.Candidates[results.CurrentState.Name] = errResult;
                        else
                            results.Candidates.Add(results.CurrentState.Name, errResult);
                    }
                }
            }
        }

        private T RunControl(IExperimentResult<TPublish> results, out Exception controlException)
        {
            controlException = null;
            CurrentState.Name = ControlName;
            CurrentState.Step = Operations.Control;
            CurrentState.Timestamp = DateTime.UtcNow;
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
                OnError(sfe.ExperimentError);
                //We'll still run the Control even without a context
            }
            var timer = new Stopwatch();
            try
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
                    Step = Operations.Control,
                    ErrorMessage = "An Exception was thrown running the Control! Exception message: "
                        + e.Message,
                    ExperimentName = ControlName
                };
                Publish(OnError(error), CurrentState.GetSnapshot());//TODO: Should we run OnError on Control??
                results.Control = new Observation<TPublish>
                {
                    Error = error,
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
                Publish(OnError(sfe.ExperimentError), CurrentState.GetSnapshot());
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
                    Step = CurrentState.Step,
                    ErrorMessage = string.Format(
                        "An Exception was thrown running Step '{0}'. Exception message: {1}",
                        CurrentState.Step, e.Message),
                    ExperimentName = CurrentState.Name
                };
                throw new StepFailedException(error);
            }
            return result;
        }

        #endregion Private Methods
    }
}