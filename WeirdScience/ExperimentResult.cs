using System.Collections.Generic;

namespace WeirdScience
{
    internal class ExperimentResult<T> : IExperimentResult<T>
    {
        #region Public Constructors

        public ExperimentResult()
        {
            CurrentState = new ExperimentState<T>();
            Candidates = new Dictionary<string, IObservation<T>>();
            Control = new Observation<T>();
        }

        #endregion Public Constructors

        #region Public Properties

        public IDictionary<string, IObservation<T>> Candidates { get; internal set; }
        public IObservation<T> Control { get; set; }
        public IExperimentState<T> CurrentState { get; internal set; }
        public string Name { get; internal set; }

        #endregion Public Properties
    }
}