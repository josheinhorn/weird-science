using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeirdScience
{
    internal class ExperimentState<T> : IExperimentState<T>
    {
        //public T CurrentValue
        //{
        //    get;
        //    set;
        //}

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

        public IExperimentState<T> GetSnapshot()
        {
            return MemberwiseClone() as IExperimentState<T>;
        }

        public DateTime Timestamp { get; set; }

    }
}
