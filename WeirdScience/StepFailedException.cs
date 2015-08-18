using System;

namespace WeirdScience
{
    public class StepFailedException : Exception
    {
        #region Public Constructors

        public StepFailedException(IExperimentError error)
         : base(string.Format("Step {0} failed", error.Step), error.LastException)
        {
            ExperimentError = error;
        }

        #endregion Public Constructors

        #region Public Properties

        public IExperimentError ExperimentError { get; private set; }

        #endregion Public Properties
    }
}