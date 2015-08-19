using System;

namespace WeirdScience
{
    internal class ExperimentState<T> : IExperimentState<T>
    {
        #region Private Fields

        private string _name;
        private Operations _step;

        #endregion Private Fields

        #region Public Properties

        public string Name
        {
            get { return _name; }
            set { _name = value; Timestamp = DateTime.UtcNow; }
        }

        public Operations CurrentStep
        {
            get { return _step; }
            set { _step = value; Timestamp = DateTime.UtcNow; }
        }

        public DateTime Timestamp { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public IExperimentState<T> Snapshot()
        {
            return MemberwiseClone() as IExperimentState<T>;
        }

        #endregion Public Methods
    }
}