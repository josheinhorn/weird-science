using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace WeirdScience
{
    public enum Operations
    {
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
        Complete,
        Internal = 0 //Exception happened in WeirdScience code, this is default
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

    public interface IExperimentOptionsBuilder<T, TPublish> : IFluentSyntax
    {
        #region Public Methods

        /// <summary>
        /// Sets a method to determine if two results are equivalent.
        /// </summary>
        /// <param name="compare"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> AreEqual(Func<T, T, bool> compare);

        /// <summary>
        /// Sets a method to determine if a set of results should be ignored for further comparison.
        /// The results will still be stored for Publish regardless.
        /// </summary>
        /// <param name="ignoreIf"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> Ignore(Func<T, T, bool> ignoreIf);

        /// <summary>
        /// Sets an action to perform if an error occurs.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> OnError(Action<IExperimentError> handler);

        /// <summary>
        /// Sets an action to perform if an error occurs and return a message.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> OnError(Func<IExperimentError, string> handler);

        /// <summary>
        /// Sets an action to perform when a mismatch occurs.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> OnMismatch(Action<T, T, Exception, Exception> handler);

        /// <summary>
        /// Sets an action to perform when a mismatch occurs and return a message.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> OnMismatch(Func<T, T, Exception, Exception, string> handler);

        /// <summary>
        /// Sets a method to determine whether or not to run the Candidates. Use this to reduce the
        /// number of times the experiment is done under high load.
        /// </summary>
        /// <param name="runIf"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> PreCondition(Func<bool> runIf);

        /// <summary>
        /// Sets a method to prepare the experiment resulsts for Publish so that unneccessary data
        /// is not held longer than necessary.
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

        IExperimentOptionsBuilder<T, TPublish> RunInParallel(Func<bool> conditional); //future!

        /// <summary>
        /// Sets a method to return a Context object that will be available in each Observation of
        /// the Experiment Results.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> SetContext(Func<object> context);

        /// <summary>
        /// Sets a method to determine the timeout in milliseconds. The Experiment will run this
        /// amount of time for each Candidate before moving on. Setting this method will force the
        /// Experiment to run each Candidate in a separate Thread. The Experiment will NOT
        /// terminate/abort the Thread after the set period and the Thread will be left to run on in
        /// Parallel with the remainder of the Program, so use this option carefully.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> SetTimeout(Func<long> timeout);

        /// <summary>
        /// Sets an action to perform before each Candidate is run.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> Setup(Action handler);

        /// <summary>
        /// Sets an action to perform before each Candidate is run and return a message.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> Setup(Func<string> handler);

        /// <summary>
        /// Sets an action to perform after each Candidate is run.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> Teardown(Action handler);

        /// <summary>
        /// Sets an action to perform after each Candidate is run and return a message.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        IExperimentOptionsBuilder<T, TPublish> Teardown(Func<string> handler);

        #endregion Public Methods
    }

    public interface IExperimentResult<T>
    {
        #region Public Properties

        IDictionary<string, IObservation<T>> Candidates { get; }
        IObservation<T> Control { get; set; }
        IExperimentState<T> LastState { get; }
        string Name { get; }

        #endregion Public Properties
    }

    public interface IExperimentState<T>
    {
        #region Public Properties

        string Name { get; set; }
        Operations CurrentStep { get; set; }

        DateTime Timestamp { get; }

        #endregion Public Properties

        #region Public Methods

        IExperimentState<T> Snapshot();

        #endregion Public Methods
    }

    public interface IExperimentSteps<T, TPublish>
    {
        #region Public Properties

        Func<T, T, bool> AreEqual { get; set; }
        IEnumerable<KeyValuePair<string, Func<T>>> GetCandidates();
        void AddCandidate(string name, Func<T> candidate);
        Func<T> Control { get; set; }
        Func<T, T, bool> Ignore { get; set; }
        Func<IExperimentError, string> OnError { get; set; }
        Func<T, T, Exception, Exception, string> OnMismatch { get; set; }
        Func<bool> PreCondition { get; set; }
        Func<T, TPublish> Prepare { get; set; }
        Action<IExperimentResult<TPublish>> Publish { get; set; }
        Func<bool> RunInParallel { get; set; }
        Func<object> SetContext { get; set; }
        Func<long> SetTimeout { get; set; }
        Func<string> Setup { get; set; }
        Func<string> Teardown { get; set; }

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

    public interface IObservation<T>
    {
        #region Public Properties

        object Context { get; }
        long ElapsedMilliseconds { get; }
        IExperimentError ExperimentError { get; }
        bool ExceptionThrown { get; }
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

        object Context();

        bool Ignore(T control, T candidate);

        string OnError(IExperimentError expError);

        string OnMismatch(T control, T candidate, Exception controlException, Exception candidateException);

        bool PreCondition();

        TPublish Prepare(T result);

        void Publish(IExperimentResult<TPublish> results);

        void Publish(string message, IExperimentState<TPublish> state);

        T Run();

        bool RunInParallel();

        string Setup();

        string Teardown();

        long Timeout();

        #endregion Public Methods
    }

    public interface ISciencePublisher //TODO: Figure out how to write messages along the process
    {
        #region Public Methods

        void Publish<T>(IExperimentResult<T> results);

        void Publish<T>(string message, IExperimentState<T> state);

        #endregion Public Methods
    }
}