using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeirdScience
{
    public static class Laboratory
    {
        private static ISciencePublisher _publisher = new ConsolePublisher();
        public static void SetPublisher(ISciencePublisher publisher)
        {
            _publisher = publisher;
        }
        public static IControlBuilder<T, T> DoScience<T>(string name, bool throwOnInternalExceptions = false)
        {
            var experiment = new SimpleExperiment<T>(name, _publisher, throwOnInternalExceptions);
            return new Laboratory<T, T>(experiment);
        }
        public static ICandidateBuilder<T, T> DoScience<T>(string name, Func<T> control,
            bool throwOnInternalExceptions = false)
        {
            var experiment = new SimpleExperiment<T>(name, _publisher, throwOnInternalExceptions);
            experiment.Steps.Control = control;
            return new CandidateBuilder<T, T>(experiment);
        }
        public static ICandidateBuilder<T, TPublish> DoScience<T, TPublish>(string name, Func<T> control,
            Func<T, TPublish> prepareResults, bool throwOnInternalExceptions = false)
        {
            //TOOD: publisher, error handler, etc
            var experiment = new Experiment<T, TPublish>(name, _publisher, throwOnInternalExceptions);
            experiment.Steps.Prepare = prepareResults;
            experiment.Steps.Control = control;
            return new CandidateBuilder<T, TPublish>(experiment);
        }

    }
    public class Laboratory<T, TPublish> : IControlBuilder<T, TPublish>
    {
        private IScienceExperiment<T, TPublish> experiment;
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

        public ICandidateBuilder<T, TPublish> Candidate(string name, Func<T> candidate)
        {
            if (experiment.Steps.Candidates.ContainsKey(name))
            {
                throw new ArgumentException("An Experiment with Name " + name + " has already been added!");
            }
            experiment.Steps.Candidates.Add(name, candidate);
            return new CandidateBuilder<T, TPublish>(experiment);
        }

        public ICandidateBuilder<T, TPublish> Control(Func<T> control)
        {
            experiment.Steps.Control = control;
            return new CandidateBuilder<T, TPublish>(experiment);
        }
    }
}
