using System;
using System.Collections.Generic;

namespace WeirdScience
{
    internal class ExceptionEqualityComparer : IEqualityComparer<Exception>
    {
        #region Public Methods

        public bool Equals(Exception x, Exception y)
        {
            if (x == null)
            {
                return y == null;
            }
            else if (y == null) return false;
            else
            {
                return x.GetType().Equals(y.GetType()) && x.Message == y.Message;
            }
        }

        public int GetHashCode(Exception obj)
        {
            return obj.GetType().GetHashCode() * 17 + obj.Message.GetHashCode();
        }

        #endregion Public Methods
    }
}