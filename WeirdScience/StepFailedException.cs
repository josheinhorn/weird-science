using System;
using System.Runtime.Serialization;

namespace WeirdScience
{
    [Serializable]
    public class StepFailedException : Exception
    {
        #region Public Constructors

        public StepFailedException(IExperimentError error)
         : base(string.Format("Step {0} failed", error.LastStep), error.LastException)
        {
            ExperimentError = error;
        }

        #endregion Public Constructors

        #region Public Properties

        public IExperimentError ExperimentError { get; private set; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
        #endregion Public Properties
    }
}