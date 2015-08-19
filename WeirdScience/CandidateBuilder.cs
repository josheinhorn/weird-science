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
            : this(experiment, new ExperimentSteps<T, TPublish>())
        { 
        }
        internal CandidateBuilder(IScienceExperiment<T, TPublish> experiment, IExperimentSteps<T, TPublish> steps)
        {
            if (experiment == null) throw new ArgumentNullException("experiment");
            if (steps == null) throw new ArgumentNullException("steps");
            this.experiment = experiment;
            this.steps = steps;
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

        public IExperimentOptionsBuilder<T, TPublish> OnError(Action<IExperimentError> handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            OnError((error) => { handler(error); return string.Empty; });
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> OnError(Func<IExperimentError, string> handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            steps.OnError = handler;
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> OnMismatch(Action<T, T, Exception, Exception> handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            OnMismatch((a, b, c, d) => { handler(a, b, c, d); return string.Empty; });
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> OnMismatch(Func<T, T, Exception, Exception, string> handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            steps.OnMismatch = handler;
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
            experiment.Steps = steps;
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

        public IExperimentOptionsBuilder<T, TPublish> Setup(Func<string> handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            steps.Setup = handler;
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> Setup(Action handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            Setup(() => { handler(); return string.Empty; });
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> Teardown(Func<string> handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            steps.Teardown = handler;
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> Teardown(Action handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            Teardown(() => { handler(); return string.Empty; });
            return this;
        }

        #endregion Public Methods
    }
}