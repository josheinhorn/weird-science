using System;
using System.Collections.Generic;

namespace WeirdScience
{
    internal class ExperimentSteps<T, TPublish> : IExperimentSteps<T, TPublish>
    {
        #region Public Constructors

        public ExperimentSteps()
        {
            Candidates = new Dictionary<string, Func<T>>();
        }

        #endregion Public Constructors

        #region Public Properties

        public Func<T, T, bool> AreEqual
        {
            get; set;
        }

        private IDictionary<string, Func<T>> Candidates
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

        public Func<IExperimentError, string> OnError
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

        public Func<T, TPublish> Prepare
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

        public Func<object> SetContext
        {
            get; set;
        }

        public Func<long> SetTimeout
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

        public void AddCandidate(string name, Func<T> candidate)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (candidate == null) throw new ArgumentNullException("candidate");
            if (!Candidates.ContainsKey(name))
            {
                Candidates.Add(name, candidate);
            }
            else
            {
                throw new ArgumentException("A Candidate with Name '" + name + "' has already been added!");
            }
        }

        public IEnumerable<KeyValuePair<string, Func<T>>> GetCandidates()
        {
            return Candidates;
        }

        #endregion Public Properties
    }
}