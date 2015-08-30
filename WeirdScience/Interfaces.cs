using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace WeirdScience
{
    public enum Operations
    {
        Internal = 0, //Exception happened in WeirdScience code, this is default
        Setup,
        PreCondition,
        Control,
        Candidate,
        Ignore,
        OnMismatch,
        OnError,
        AreEqual,
        SetContext,
        Publish,
        RunInParallel,
        SetTimeout,
        Prepare,
        Teardown,
        Complete
    }

    public interface ICandidateBuilder<T, TPublish> : IFluentSyntax
    {
        #region Public Methods

        /// <summary>
        /// Adds a Candidate to the Experiment.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="candidate"></param>
        /// <returns></returns>
        IExperimentBuilder<T, TPublish> Candidate(string name, Func<T> candidate);

        #endregion Public Methods
    }

    public interface IControlBuilder<T, TPublish> : ICandidateBuilder<T, TPublish>
    {
        #region Public Methods

        IExperimentBuilder<T, TPublish> Control(Func<T> control);

        #endregion Public Methods
    }
    public interface ILaboratory
    {
        IControlBuilder<T, TPublish> CreateExperiment<T, TPublish>(string experimentName);
        IControlBuilder<T, TPublish> CreateExperiment<T, TPublish>(IScienceExperiment<T, TPublish> custom);

    }
    public interface IErrorEventArgs : IExperimentEventArgs
    {
        #region Public Properties

        IExperimentError ExperimentError { get; }

        #endregion Public Properties
    }

    public interface IErrorHandler
    {
        #region Public Methods

        void HandleError(IExperimentError expError);

        #endregion Public Methods
    }

    public interface IExperimentBuilder<T, TPublish> : IExperimentOptionsBuilder<T, TPublish>,
                    ICandidateBuilder<T, TPublish>
    {
    }

    public interface IExperimentError
    {
        #region Public Properties

        string ErrorMessage { get; }
        string ExperimentName { get; }
        Exception LastException { get; }
        Operations LastStep { get; }

        #endregion Public Properties
    }

    public interface IExperimentEventArgs
    {
        #region Public Properties

        ISciencePublisher Publisher { get; }
        IExperimentState State { get; }

        #endregion Public Properties
    }

    public interface IExperimentOptionsBuilder<T, TPublish> : IFluentSyntax
    {
        #region Public Methods

        /// <summary>
        /// Sets a method to determine if two results are equivalent with the signature:
        /// bool function(T control, T candidate)
        /// </summary>
        /// <param name="compare"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> AreEqual(Func<T, T, bool> compare);

        /// <summary>
        /// Sets a method to determine if a set of results should be ignored for further comparison.
        /// The results will still be stored for Publish regardless. The method should have the signature:
        /// bool function(T control, T candidate)
        /// </summary>
        /// <param name="ignoreIf"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> Ignore(Func<T, T, bool> ignoreIf);

        /// <summary>
        /// Sets an action to perform if an error occurs.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        //IExperimentOptionsBuilder<T, TPublish> OnError(Action<IExperimentError> handler);

        /// <summary>
        /// Sets an action to perform if an error occurs. Multiple handlers can be added.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> OnError(EventHandler<ErrorEventArgs> handler);

        /// <summary>
        /// Sets an action to perform when a mismatch occurs. Multiple handlers can be added.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> OnMismatch(EventHandler<MismatchEventArgs<T>> handler);

        /// <summary>
        /// Sets a method to determine whether or not to run the Candidates. Use this to conditionally run 
        /// Experiments and reduce added load.
        /// </summary>
        /// <param name="runIf"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> PreCondition(Func<bool> runIf);

        /// <summary>
        /// Sets a method to prepare the experiment resulsts for Publish so that unneccessary data
        /// is not persisted in memory.
        /// </summary>
        /// <remarks>
        /// Common uses would be to extract only the data you care to publish from a result,
        /// especially in the case of lists of large objects.
        /// </remarks>
        /// <param name="prepare"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> Prepare(Func<T, TPublish> prepare);

        /// <summary>
        /// Runs the Experiment and returns the result from the Control or throws the Exception that
        /// the Control throws.
        /// </summary>
        /// <returns></returns>
        T Run();

        /// <summary>
        /// Not currently implemented.
        /// </summary>
        /// <param name="conditional"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> RunInParallel(Func<bool> conditional); //future!

        /// <summary>
        /// Sets a method to return a Context object that will be available in each Observation of
        /// the Experiment Results.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> SetContext(Func<object> context);

        /// <summary>
        /// Not currently implemented. Sets a method to determine the timeout in milliseconds. The
        /// Experiment will run this amount of time for each Candidate before moving on. Setting
        /// this method will force the Experiment to run each Candidate in a separate Thread. The
        /// Experiment will NOT terminate/abort the Thread after the set period and the Thread will
        /// be left to run on in Parallel with the remainder of the Program, so use this option carefully.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> SetTimeout(Func<long> timeout);

        /// <summary>
        /// Sets an action to perform before each Candidate is run. Multiple handlers can be added.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> Setup(EventHandler<ExperimentEventArgs> handler);

        /// <summary>
        /// Sets an action to perform after each Candidate is run. Multiple handlers can be added.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> Teardown(EventHandler<ExperimentEventArgs> handler);

        #endregion Public Methods
    }

    public interface IExperimentResult<T>
    {
        #region Public Properties

        IDictionary<string, IObservation<T>> Candidates { get; }
        IObservation<T> Control { get; set; }
        IExperimentState LastState { get; }
        string Name { get; }

        #endregion Public Properties
    }

    public interface IExperimentState
    {
        #region Public Properties

        object Context { get; set; }
        Operations CurrentStep { get; set; }
        string Name { get; set; }
        DateTime Timestamp { get; }

        #endregion Public Properties

        #region Public Methods

        IExperimentState Snapshot();

        #endregion Public Methods
    }

    public interface IExperimentSteps<T, TPublish>
    {
        #region Public Properties

        event EventHandler<ErrorEventArgs> OnErrorEvent;
        void OnError(ErrorEventArgs args);

        event EventHandler<MismatchEventArgs<T>> OnMismatchEvent;

        event EventHandler<ExperimentEventArgs> SetupEvent;

        event EventHandler<ExperimentEventArgs> TeardownEvent;
        void OnMismatch(MismatchEventArgs<T> args);

        void Setup(ExperimentEventArgs args);

        void Teardown(ExperimentEventArgs args);
        Func<T, T, bool> AreEqual { get; set; }
        Func<T> Control { get; set; }

        Func<T, T, bool> Ignore { get; set; }

        Func<bool> PreCondition { get; set; }

        Func<T, TPublish> Prepare { get; set; }

        Action<IExperimentResult<TPublish>> Publish { get; set; }

        Func<bool> RunInParallel { get; set; }

        Func<object> SetContext { get; set; }

        Func<long> SetTimeout { get; set; }

        void AddCandidate(string name, Func<T> candidate);

        IEnumerable<KeyValuePair<string, Func<T>>> GetCandidates();

        #endregion Public Properties
    }

    /// <summary>
    /// Hides System.Object methods
    /// </summary>
    /// <remarks>Credit to Daniel Cazzulino http://bit.ly/ifluentinterface</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IFluentSyntax
    {
        #region Public Methods

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool Equals(object obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        int GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        Type GetType();

        [EditorBrowsable(EditorBrowsableState.Never)]
        string ToString();

        #endregion Public Methods
    }

    public interface IMismatchEventArgs<T> : IExperimentEventArgs
    {
        #region Public Properties

        T Candidate { get; }
        Exception CandidateException { get; }
        T Control { get; }
        Exception ControlException { get; }

        #endregion Public Properties
    }

    public interface IObservation<T>
    {
        #region Public Properties

        object Context { get; }
        long ElapsedMilliseconds { get; }
        bool ExceptionThrown { get; }
        IExperimentError ExperimentError { get; }
        bool IsMismatched { get; }
        string Name { get; }
        bool TimedOut { get; }
        T Value { get; }

        #endregion Public Properties

        //other diagnostic info
    }

    public interface IScienceExperiment<T, TPublish>
    {
        #region Public Properties

        string Name { get; }

        //IExperimentState<TPublish> CurrentState { get; set; }
        IExperimentSteps<T, TPublish> Steps { get; set; }

        #endregion Public Properties

        #region Public Methods

        bool AreEqual(T control, T candidate);

        bool Ignore(T control, T candidate);

        void OnError(ErrorEventArgs args);

        void OnMismatch(MismatchEventArgs<T> args);

        bool PreCondition();

        TPublish Prepare(T result);

        void Publish(IExperimentResult<TPublish> results);

        void Publish(string message, IExperimentState state);

        T Run();

        bool RunInParallel();

        object SetContext();

        long SetTimeout();

        void Setup(ExperimentEventArgs args);

        void Teardown(ExperimentEventArgs args);

        #endregion Public Methods
    }

    public interface ISciencePublisher //TODO: Figure out how to write messages along the process
    {
        #region Public Methods

        void Publish<T>(IExperimentResult<T> results);

        void Publish(string message, IExperimentState state);

        #endregion Public Methods
    }
}