using System;

namespace WeirdScience
{
    public class Laboratory : ILaboratory
    {
        #region Private Static Fields

        private static ISciencePublisher _publisher = new ConsolePublisher();

        #endregion Private Static Fields

        #region Public Static Methods

        public static IControlBuilder<T, T> DoScience<T>(string name, bool throwOnInternalExceptions = false)
        {
            return new Laboratory(_publisher, throwOnInternalExceptions)
                .CreateExperiment<T, T>(name);
        }

        public static IExperimentBuilder<T, T> DoScience<T>(string name, Func<T> control,
            bool throwOnInternalExceptions = false)
        {
            return new Laboratory(_publisher, throwOnInternalExceptions)
                .CreateExperiment<T, T>(name)
                .Control(control);
        }

        public static IExperimentBuilder<T, TPublish> DoScience<T, TPublish>(string name, Func<T> control,
            PrepareDelegate<T, TPublish> prepareResults, bool throwOnInternalExceptions = false)
        {
            var builder = new Laboratory(_publisher, throwOnInternalExceptions)
                .CreateExperiment<T, TPublish>(name)
                .Control(control);
            builder.Prepare(prepareResults);
            return builder;
        }

        public static void SetPublisher(ISciencePublisher publisher)
        {
            _publisher = publisher;
        }

        #endregion Public Static Methods

        #region Private Fields

        private readonly ISciencePublisher publisher;
        private readonly bool throwOnInternalExceptions;

        #endregion Private Fields

        #region Public Constructors

        //public Laboratory(IScienceExperiment<T, TPublish> experiment, IExperimentSteps<T, TPublish> steps)
        //{
        //    if (experiment == null) throw new ArgumentNullException("experiment");
        //    if (steps == null) throw new ArgumentNullException("steps");
        //    this.experiment = experiment;
        //    this.steps = steps;
        //}
        //public Laboratory(IScienceExperiment<T, TPublish> experiment)
        //    : this(experiment, new ExperimentSteps<T,TPublish>())
        //{ }

        //public Laboratory(string name) : this(name, new ConsolePublisher())
        //{ }

        public Laboratory(ISciencePublisher publisher)
            : this(publisher, false)
        { }

        //ctor for testing
        public Laboratory(ISciencePublisher publisher, bool throwOnInternalExceptions)
        {
            if (publisher == null) throw new ArgumentNullException("publisher");
            this.publisher = publisher;
            this.throwOnInternalExceptions = throwOnInternalExceptions;
        }

        #endregion Public Constructors

        #region Public Methods

        public IControlBuilder<T, TPublish> CreateExperiment<T, TPublish>(string experimentName)
        {
            return CreateExperiment(new Experiment<T, TPublish>(experimentName, publisher, throwOnInternalExceptions));
        }

        public IControlBuilder<T, TPublish> CreateExperiment<T, TPublish>(IScienceExperiment<T, TPublish> custom)
        {
            return new ControlBuilder<T, TPublish>(custom);
        }

        #endregion Public Methods
    }
}