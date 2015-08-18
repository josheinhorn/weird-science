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
            var experiment = new SimpleExperiment<T>(name, _publisher, throwOnInternalExceptions);
            return new Laboratory<T, T>(experiment);
        }

        public static IExperimentBuilder<T, T> DoScience<T>(string name, Func<T> control,
            bool throwOnInternalExceptions = false)
        {
            var experiment = new SimpleExperiment<T>(name, _publisher, throwOnInternalExceptions);
            experiment.Steps.Control = control;
            return new CandidateBuilder<T, T>(experiment);
        }

        public static IExperimentBuilder<T, TPublish> DoScience<T, TPublish>(string name, Func<T> control,
            Func<T, TPublish> prepareResults, bool throwOnInternalExceptions = false)
        {
            //TOOD: publisher, error handler, etc
            var experiment = new Experiment<T, TPublish>(name, _publisher, throwOnInternalExceptions);
            experiment.Steps.Prepare = prepareResults;
            experiment.Steps.Control = control;
            return new CandidateBuilder<T, TPublish>(experiment);
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

        #endregion Private Fields

        #region Public Constructors

        public Laboratory(IScienceExperiment<T, TPublish> experiment)
        {
            this.experiment = experiment;
        }

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
            if (experiment.Steps.Candidates.ContainsKey(name))
            {
                throw new ArgumentException("An Experiment with Name " + name + " has already been added!");
            }
            experiment.Steps.Candidates.Add(name, candidate);
            return new CandidateBuilder<T, TPublish>(experiment);
        }

        public IExperimentBuilder<T, TPublish> Control(Func<T> control)
        {
            experiment.Steps.Control = control;
            return new CandidateBuilder<T, TPublish>(experiment);
        }

        #endregion Public Methods
    }
}