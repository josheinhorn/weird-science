using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeirdScience
{
    public class MismatchEventArgs<T> : ExperimentEventArgs, IMismatchEventArgs<T>
    {
        public T Candidate
        {
            get; internal set;
        }

        public Exception CandidateException
        {
            get; internal set;
        }

        public T Control
        {
            get; internal set;
        }

        public Exception ControlException
        {
            get; internal set;
        }
    }
}
