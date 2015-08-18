using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeirdScience
{
    internal class ExperimentSteps<T, TPublish> : IExperimentSteps<T, TPublish>
    {
        public ExperimentSteps()
        {
            Candidates = new Dictionary<string, Func<T>>();
        }

        public Func<T, T, bool> AreEqual
        {
            get; set;
        }

        public IDictionary<string, Func<T>> Candidates
        {
            get; private set;
        }

        public Func<object> SetContext
        {
            get; set;
        }

        public Func<T> Control
        {
            get; set;
        }

        public Func<T, T, bool> Ignore
        {
            get; set;
        }

        public Func<T, T, Exception, Exception, string> OnMismatch
        {
            get; set;
        }

        public Func<bool> PreCondition
        {
            get; set;
        }

        public Action<IExperimentResult<TPublish>> Publish
        {
            get; set;
        }

        public Func<bool> RunInParallel
        {
            get; set;
        }

        public Func<T, TPublish> Prepare
        {
            get; set;
        }

        public Func<string> Setup
        {
            get; set;
        }

        public Func<string> Teardown
        {
            get; set;
        }

        public Func<long> SetTimeout
        {
            get; set;
        }

        public Func<IExperimentError, string> OnError
        {
            get; set;
        }
    }
}
