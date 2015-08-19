using System;

namespace WeirdScience
{
    internal class ExperimentError : IExperimentError
    {
        #region Public Properties

        public string ErrorMessage
        {
            get; internal set;
        }

        public string ExperimentName
        {
            get; internal set;
        }

        public Exception LastException
        {
            get; internal set;
        }

        public Operations LastStep
        {
            get; internal set;
        }

        #endregion Public Properties
    }
}