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

        public event EventHandler<ErrorEventArgs> OnErrorEvent;

        public event EventHandler<MismatchEventArgs<T>> OnMismatchEvent;

        public event EventHandler<ExperimentEventArgs> SetupEvent;

        public event EventHandler<ExperimentEventArgs> TeardownEvent;

        public Func<T, T, bool> AreEqual
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

        private IDictionary<string, Func<T>> Candidates
        {
            get; set;
        }

        #endregion Public Properties

        #region Public Methods

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

        public void OnError(ErrorEventArgs args)
        {
            var handler = OnErrorEvent;
            if (handler != null)
                handler(this, args);
        }

        public void OnMismatch(MismatchEventArgs<T> args)
        {
            var handler = OnMismatchEvent;
            if (handler != null)
                handler(this, args);
        }

        public void Setup(ExperimentEventArgs args)
        {
            var handler = SetupEvent;
            if (handler != null)
                handler(this, args);
        }

        public void Teardown(ExperimentEventArgs args)
        {
            var handler = TeardownEvent;
            if (handler != null)
                handler(this, args);
        }

        #endregion Public Methods
    }
}