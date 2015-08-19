using System;

namespace WeirdScience
{
    public static class Laboratory
    {
        #region Private Fields

        private static ISciencePublisher _publisher = new ConsolePublisher();

        #endregion Private Fields

        #region Public Methods

        public static IControlBuilder<T, T> DoScience<T>(string name, bool throwOnInternalExceptions = false)
        {
            return new Laboratory<T, T>(name, _publisher, throwOnInternalExceptions);
        }

        public static IExperimentBuilder<T, T> DoScience<T>(string name, Func<T> control,
            bool throwOnInternalExceptions = false)
        {
            return new Laboratory<T, T>(name, _publisher, throwOnInternalExceptions)
                .Control(control);
        }

        public static IExperimentBuilder<T, TPublish> DoScience<T, TPublish>(string name, Func<T> control,
            Func<T, TPublish> prepareResults, bool throwOnInternalExceptions = false)
        {
            var builder = new Laboratory<T, TPublish>(name, _publisher, throwOnInternalExceptions)
                .Control(control);
            builder.Prepare(prepareResults);
            return builder;
        }

        public static void SetPublisher(ISciencePublisher publisher)
        {
            _publisher = publisher;
        }

        #endregion Public Methods
    }

    public class Laboratory<T, TPublish> : IControlBuilder<T, TPublish>
    {
        #region Private Fields

        private IScienceExperiment<T, TPublish> experiment;
        private IExperimentSteps<T, TPublish> steps;
        #endregion Private Fields

        #region Public Constructors
        public Laboratory(IScienceExperiment<T, TPublish> experiment, IExperimentSteps<T, TPublish> steps)
        {
            if (experiment == null) throw new ArgumentNullException("experiment");
            if (steps == null) throw new ArgumentNullException("steps");
            this.experiment = experiment;
            this.steps = steps;
        }
        public Laboratory(IScienceExperiment<T, TPublish> experiment) 
            : this(experiment, new ExperimentSteps<T,TPublish>())
        { }

        public Laboratory(string name) : this(name, new ConsolePublisher())
        { }

        public Laboratory(string name, ISciencePublisher publisher)
            : this(new Experiment<T, TPublish>(name, publisher, false))
        { }

        public Laboratory(string name, ISciencePublisher publisher, bool throwOnInternalExceptions)
            : this(new Experiment<T, TPublish>(name, publisher, throwOnInternalExceptions))
        { }

        #endregion Public Constructors

        #region Public Methods

        public IExperimentBuilder<T, TPublish> Candidate(string name, Func<T> candidate)
        {
            steps.AddCandidate(name, candidate);
            return new CandidateBuilder<T, TPublish>(experiment, steps);
        }

        public IExperimentBuilder<T, TPublish> Control(Func<T> control)
        {
            steps.Control = control;
            return new CandidateBuilder<T, TPublish>(experiment, steps);
        }

        #endregion Public Methods
    }
}