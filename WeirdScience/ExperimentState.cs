using System;

namespace WeirdScience
{
    internal class ExperimentState<T> : IExperimentState<T>
    {
        //public T CurrentValue
        //{
        //    get;
        //    set;
        //}

        #region Public Properties

        public string Name
        {
            get;
            set;
        }

        public Operations Step
        {
            get;
            set;
        }

        public DateTime Timestamp { get; set; }

        #endregion Public Properties

        #region Public Methods

        public IExperimentState<T> GetSnapshot()
        {
            return MemberwiseClone() as IExperimentState<T>;
        }

        #endregion Public Methods
    }
}