using System;

namespace WeirdScience
{
    internal class CandidateBuilder<T, TPublish> : IExperimentBuilder<T, TPublish>
    {
        #region Private Fields

        private IScienceExperiment<T, TPublish> experiment;
        private IExperimentSteps<T, TPublish> steps;
        #endregion Private Fields

        #region Public Constructors

        internal CandidateBuilder(IScienceExperiment<T, TPublish> experiment) 
        {
            this.experiment = experiment;
            steps = experiment.Steps;
        }

        #endregion Public Constructors

        #region Public Methods

        public IExperimentOptionsBuilder<T, TPublish> AreEqual(Func<T, T, bool> compare)
        {
            if (compare == null) throw new ArgumentNullException("compare");
            steps.AreEqual = compare;
            return this;
        }

        public IExperimentBuilder<T, TPublish> Candidate(string name, Func<T> candidate)
        {
            if (candidate == null) throw new ArgumentNullException("candidate");
            if (name == null) throw new ArgumentNullException("name");
            steps.AddCandidate(name, candidate);
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> Ignore(Func<T, T, bool> ignoreIf)
        {
            if (ignoreIf == null) throw new ArgumentNullException("ignoreIf");
            steps.Ignore = ignoreIf;
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> OnError(EventHandler<ErrorEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            steps.OnErrorEvent += handler;
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> OnMismatch(EventHandler<MismatchEventArgs<T>> handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            steps.OnMismatchEvent += handler;
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> PreCondition(Func<bool> runIf)
        {
            if (runIf == null) throw new ArgumentNullException("runIf");
            steps.PreCondition = runIf;
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> Prepare(Func<T, TPublish> prepare)
        {
            if (prepare == null) throw new ArgumentNullException("prepare");
            steps.Prepare = prepare;
            return this;
        }

        public T Run()
        {
            return experiment.Run();
        }

        public IExperimentOptionsBuilder<T, TPublish> RunInParallel(Func<bool> conditional)
        {
            if (conditional == null) throw new ArgumentNullException("conditional");
            steps.RunInParallel = conditional;
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> SetContext(Func<object> context)
        {
            if (context == null) throw new ArgumentNullException("context");
            steps.SetContext = context;
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> SetTimeout(Func<long> timeout)
        {
            if (timeout == null) throw new ArgumentNullException("timeout");
            steps.SetTimeout = timeout;
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> Setup(EventHandler<ExperimentEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            steps.SetupEvent += handler;
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> Teardown(EventHandler<ExperimentEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            steps.TeardownEvent += handler;
            return this;
        }

        #endregion Public Methods
    }
}