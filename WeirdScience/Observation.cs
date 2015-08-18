using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeirdScience
{
    internal class Observation<T> : IObservation<T>
    {
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

        public IExperimentError Error
        {
            get;
            internal set;
        }

        public bool ExceptionThrown
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
    }
}
