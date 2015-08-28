using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeirdScience
{
    public class ExperimentEventArgs : IExperimentEventArgs
    {
        public ISciencePublisher Publisher
        {
            get; internal set;
        }

        public IExperimentState State
        {
            get; internal set;
        }
    }
}
