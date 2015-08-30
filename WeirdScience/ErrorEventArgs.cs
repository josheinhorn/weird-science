using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeirdScience
{
    public class ErrorEventArgs : ExperimentEventArgs, IErrorEventArgs
    {
        public IExperimentError ExperimentError
        {
            get; internal set;
        }
    }
}
