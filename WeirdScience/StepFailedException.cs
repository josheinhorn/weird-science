using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeirdScience
{
    public class StepFailedException : Exception
    {
        public IExperimentError ExperimentError { get; private set; }
        public StepFailedException(IExperimentError error)
         : base(string.Format("Step {0} failed", error.Step), error.LastException)
        {
            ExperimentError = error;
        }
    }
}
