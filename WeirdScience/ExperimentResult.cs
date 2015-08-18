using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeirdScience
{
    internal class ExperimentResult<T> : IExperimentResult<T>
    {
        public ExperimentResult()
        {
            CurrentState = new ExperimentState<T>();
            Candidates = new Dictionary<string, IObservation<T>>();
            Control = new Observation<T>();
        }
        public IObservation<T> Control { get; set; }
        public IDictionary<string, IObservation<T>> Candidates { get; internal set; }
        public IExperimentState<T> CurrentState { get; internal set; }
        public string Name { get; internal set; }
    }
}
