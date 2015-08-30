namespace WeirdScience
{
    public class ErrorEventArgs : ExperimentEventArgs, IErrorEventArgs
    {
        #region Public Properties

        public IExperimentError ExperimentError
        {
            get; internal set;
        }

        #endregion Public Properties
    }
}