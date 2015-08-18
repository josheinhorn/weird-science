using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeirdScience
{
    internal class CandidateBuilder<T, TPublish> : ICandidateBuilder<T, TPublish>
    {
        private IScienceExperiment<T, TPublish> experiment;

        public CandidateBuilder(IScienceExperiment<T, TPublish> experiment)
        {
            if (experiment == null) throw new ArgumentNullException("experiment");
            this.experiment = experiment;
        }
        public IExperimentOptionsBuilder<T, TPublish> AreEqual(Func<T, T, bool> compare)
        {
            if (compare == null) throw new ArgumentNullException("compare");
            experiment.Steps.AreEqual = compare;
            return this;
        }

        public ICandidateBuilder<T, TPublish> Candidate(string name, Func<T> candidate)
        {
            if (candidate == null) throw new ArgumentNullException("candidate");
            if (name == null) throw new ArgumentNullException("name");
            if (experiment.Steps.Candidates.ContainsKey(name))
            {
                throw new ArgumentException("An Experiment with Name " + name + " has already been added!");
            }
            experiment.Steps.Candidates.Add(name, candidate);
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> SetContext(Func<object> context)
        {
            if (context == null) throw new ArgumentNullException("context");
            experiment.Steps.SetContext = context;
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> Ignore(Func<T, T, bool> ignoreIf)
        {
            if (ignoreIf == null) throw new ArgumentNullException("ignoreIf");
            experiment.Steps.Ignore = ignoreIf;
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
            experiment.Steps.OnError = handler;
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
            experiment.Steps.OnMismatch = handler;
            return this;
        }
        public IExperimentOptionsBuilder<T, TPublish> PreCondition(Func<bool> runIf)
        {
            if (runIf == null) throw new ArgumentNullException("runIf");
            experiment.Steps.PreCondition = runIf;
            return this;
        }

        public T Run()
        {
            return experiment.Run();
        }

        public IExperimentOptionsBuilder<T, TPublish> RunInParallel(Func<bool> conditional)
        {
            if (conditional == null) throw new ArgumentNullException("conditional");
            experiment.Steps.RunInParallel = conditional;
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> Prepare(Func<T, TPublish> prepare)
        {
            if (prepare == null) throw new ArgumentNullException("prepare");
            experiment.Steps.Prepare = prepare;
            return this;
        }
        public IExperimentOptionsBuilder<T, TPublish> Setup(Func<string> handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            experiment.Steps.Setup = handler;
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> Teardown(Func<string> handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            experiment.Steps.Teardown = handler;
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> Setup(Action handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            Setup(() => { handler(); return string.Empty; });
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> Teardown(Action handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            Teardown(() => { handler(); return string.Empty; });
            return this;
        }

        public IExperimentOptionsBuilder<T, TPublish> SetTimeout(Func<long> timeout)
        {
            if (timeout == null) throw new ArgumentNullException("timeout");
            experiment.Steps.SetTimeout = timeout;
            return this;
        }
    }
}
