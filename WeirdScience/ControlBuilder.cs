using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeirdScience
{
    internal class ControlBuilder<T, TPublish> : IControlBuilder<T, TPublish>
    {
        private IExperimentSteps<T, TPublish> steps;
        private IScienceExperiment<T, TPublish> experiment;

        internal ControlBuilder(IScienceExperiment<T, TPublish> experiment)
        {
            this.experiment = experiment;
            steps = experiment.Steps;
        }
        
        public IExperimentBuilder<T, TPublish> Candidate(string name, Func<T> candidate)
        {
            steps.AddCandidate(name, candidate);
            return new CandidateBuilder<T, TPublish>(experiment);
        }

        public IExperimentBuilder<T, TPublish> Control(Func<T> control)
        {
            steps.Control = control;
            return new CandidateBuilder<T, TPublish>(experiment);
        }

    }
}
