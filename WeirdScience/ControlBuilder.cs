using System;

namespace WeirdScience
{
    internal class ControlBuilder<T, TPublish> : IControlBuilder<T, TPublish>
    {
        #region Private Fields

        private IScienceExperiment<T, TPublish> experiment;
        private IExperimentSteps<T, TPublish> steps;

        #endregion Private Fields

        #region Internal Constructors

        internal ControlBuilder(IScienceExperiment<T, TPublish> experiment)
        {
            this.experiment = experiment;
            steps = experiment.Steps;
        }

        #endregion Internal Constructors

        #region Public Methods

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

        #endregion Public Methods
    }
}