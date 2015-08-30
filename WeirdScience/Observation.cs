namespace WeirdScience
{
    internal class Observation<T> : IObservation<T>
    {
        #region Public Properties

        public object Context
        {
            get;
            internal set;
        }

        public long ElapsedMilliseconds
        {
            get;
            internal set;
        }

        public bool ExceptionThrown
        {
            get;
            internal set;
        }

        public IExperimentError ExperimentError
        {
            get;
            internal set;
        }

        public bool IsMismatched
        {
            get;
            internal set;
        }

        public string Name
        {
            get;
            internal set;
        }

        public bool TimedOut
        {
            get;
            internal set;
        }

        public T Value
        {
            get;
            internal set;
        }

        #endregion Public Properties
    }
}