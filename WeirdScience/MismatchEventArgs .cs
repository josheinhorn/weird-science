using System;

namespace WeirdScience
{
    public class MismatchEventArgs<T> : ExperimentEventArgs, IMismatchEventArgs<T>
    {
        #region Public Properties

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

        #endregion Public Properties
    }
}