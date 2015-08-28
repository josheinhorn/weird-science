using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WeirdScience
{
    // Consider state machine or state pattern? State pattern would make it harder to extend right?
    // Experiment is the primary point of Extensibility

    // TODO: Move all error handling out of the Steps methods so extenders 
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
            return Steps.AreEqual == null ?
                EqualityComparer<T>.Default.Equals(control, candidate)
                : Steps.AreEqual(control, candidate);
        }

        public virtual object SetContext()
        {
            return Steps.SetContext != null ? Steps.SetContext() : null;
        }

        public virtual bool Ignore(T control, T candidate)
        {
            return Steps.Ignore == null ? false : Steps.Ignore(control, candidate);
        }

        public virtual string OnError(IExperimentError error)
        {
            return Steps.OnError != null ? Steps.OnError(error) : string.Empty;
        }

        public virtual string OnMismatch(T control, T candidate,
            Exception controlException, Exception candidateException)
        {
            return Steps.OnMismatch != null ?
                Steps.OnMismatch(control, candidate, controlException, candidateException)
                : string.Empty;
        }

        public virtual bool PreCondition()
        {
            return Steps.PreCondition == null ? true : Steps.PreCondition();
        }

        public virtual TPublish Prepare(T result)
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
        }

        public virtual void Publish(IExperimentResult<TPublish> results)
        {
            if (Publisher != null)
                Publisher.Publish(results);
        }

        public virtual void Publish(string message, IExperimentState<TPublish> state)
        {
            if (Publisher != null)
                Publisher.Publish(message, state);
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
                TryPublish(results);
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
            return Steps.RunInParallel == null ? false : Steps.RunInParallel();
        }

        public virtual string Setup()
        {
            return Steps.Setup != null ? Steps.Setup() : string.Empty;
        }

        public virtual string Teardown()
        {
            return Steps.Teardown != null ? Steps.Teardown() : string.Empty;
        }

        public virtual long SetTimeout()
        {
            return Steps.SetTimeout == null ? 0 : Steps.SetTimeout();
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
                        if (TryPreCondition() && candidate.Value != null)
                        {
                            var context = TrySetContext();
                            TrySetupAndPublish();
                            timer.Restart();
                            candResult = TryCandidate(candidate.Value);
                            timer.Stop();
                            if (!TryIgnore(controlResult, candResult) &&
                                (controlException != null || !TryAreEqual(controlResult, candResult)))
                            {
                                mismatched = true;
                                TryOnMismatchAndPublish(controlResult, candResult, controlException, null);
                            }
                            candValue = TryPrepare(candResult);
                            TryTeardownAndPublish();
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
                        TryOnErrorAndPublish(sfe.ExperimentError);
                        if (!exceptionComparer.Equals(sfe.ExperimentError.LastException, controlException))
                        {
                            //It's a mismatch if the exceptions don't match
                            mismatched = true;
                            TryOnMismatchAndPublish(controlResult, candResult,
                                controlException, sfe.ExperimentError.LastException); //potential throw
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
            IExperimentError stepError = null;
            object context = null;
            T result = default(T);
            TPublish value = default(TPublish);
            try
            {
                context = TrySetContext();
            }
            catch (StepFailedException sfe)
            {
                stepError = sfe.ExperimentError;
                TryOnErrorAndPublish(stepError);
            }
            try
            {
                TrySetupAndPublish();
            }
            catch (StepFailedException sfe)
            {
                stepError = sfe.ExperimentError;
                TryOnErrorAndPublish(stepError);
            }
            var timer = new Stopwatch();
            CurrentState.CurrentStep = Operations.Control;
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
                    LastStep = Operations.Control,
                    ErrorMessage = "An Exception was thrown running the Control! Exception message: "
                        + e.Message,
                    ExperimentName = ControlName
                };
                TryOnErrorAndPublish(error);//TODO: Should we run OnError on Control??
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
            try
            {
                value = TryPrepare(result);
            }
            catch (StepFailedException sfe)
            {
                stepError = sfe.ExperimentError;
                TryOnErrorAndPublish(stepError);
                //We must continue on because we've already gotten a result
            }
            try
            {
                TryTeardownAndPublish();
            }
            catch (StepFailedException sfe)
            {
                stepError = sfe.ExperimentError;
                TryOnErrorAndPublish(stepError);
            }
            results.Control = new Observation<TPublish>
            {
                ElapsedMilliseconds = timer.ElapsedMilliseconds,
                Context = context,
                Name = ControlName,
                IsMismatched = false, //Control can't be mismatched
                ExceptionThrown = stepError != null,
                ExperimentError = stepError,
                Value = value
            };
            return result;
        }

        private TOut TryOp<TOut>(Func<TOut> tryOp)
        {
            try
            {
                return tryOp();
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
        }

        #region Try Steps
        private T TryCandidate(Func<T> candidate)
        {
            CurrentState.CurrentStep = Operations.Candidate;
            return TryOp(candidate);
        }
        private bool TryAreEqual(T control, T candidate)
        {
            CurrentState.CurrentStep = Operations.AreEqual;
            return TryOp(() => AreEqual(control, candidate));
        }

        private object TrySetContext()
        {
            CurrentState.CurrentStep = Operations.SetContext;
            return TryOp(SetContext);
        }

        private bool TryIgnore(T control, T candidate)
        {
            CurrentState.CurrentStep = Operations.Ignore;
            return TryOp(() => Ignore(control, candidate));
        }

        private void TryOnErrorAndPublish(IExperimentError error)
        {
            if (error == null) throw new ArgumentNullException("error");
            CurrentState.CurrentStep = Operations.OnError;
            var msg = TryOp(() => OnError(error));
            TryPublish(msg, CurrentState.Snapshot());
        }

        private void TryOnMismatchAndPublish(T control, T candidate,
            Exception controlException, Exception candidateException)
        {
            CurrentState.CurrentStep = Operations.OnMismatch;
            var msg = TryOp(() => OnMismatch(control, candidate, controlException, candidateException));
            TryPublish(msg, CurrentState.Snapshot());
        }

        private bool TryPreCondition()
        {
            CurrentState.CurrentStep = Operations.PreCondition;
            return TryOp(PreCondition);
        }

        private TPublish TryPrepare(T result)
        {
            CurrentState.CurrentStep = Operations.Prepare;
            return TryOp(() => Prepare(result));
        }

        private void TryPublish(IExperimentResult<TPublish> results)
        {
            CurrentState.CurrentStep = Operations.Publish;
            TryOp(() => { Publish(results); return 0; });
        }

        private void TryPublish(string message, IExperimentState<TPublish> state)
        {
            CurrentState.CurrentStep = Operations.Publish;
            TryOp(() => { Publish(message, state); return 0; });
        }

        private bool TryRunInParallel()
        {
            CurrentState.CurrentStep = Operations.RunInParallel;
            return TryOp(RunInParallel);
        }

        private void TrySetupAndPublish()
        {
            CurrentState.CurrentStep = Operations.Setup;
            var msg = TryOp(Setup);
            TryPublish(msg, CurrentState.Snapshot());
        }

        private void TryTeardownAndPublish()
        {
            CurrentState.CurrentStep = Operations.Teardown;
            var msg = TryOp(Teardown);
            TryPublish(msg, CurrentState.Snapshot());
        }

        private long TrySetTimeout()
        {
            CurrentState.CurrentStep = Operations.SetTimeout;
            return TryOp(SetTimeout);
        }
        #endregion Try Steps

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