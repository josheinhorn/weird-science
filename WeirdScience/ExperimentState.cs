using System;

namespace WeirdScience
{
    internal class ExperimentState<T> : IExperimentState
    {
        #region Private Fields

        private string _name;
        private Operations _step;
        private object _context;
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

        public object Context
        {
            get { return _context; }
            set { _context = value; Timestamp = DateTime.UtcNow; }
        }

        #endregion Public Properties

        #region Public Methods

        public IExperimentState Snapshot()
        {
            return MemberwiseClone() as IExperimentState;
        }

        #endregion Public Methods
    }
}