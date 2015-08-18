using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeirdScience
{
    internal class ExperimentError : IExperimentError
    {
        public string ErrorMessage
        {
            get; internal set;
        }

        public string ExperimentName
        {
            get; internal set;
        }

        public Exception LastException
        {
            get; internal set;
        }

        public Operations Step
        {
            get; internal set;
        }
    }
}
