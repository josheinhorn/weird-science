using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeirdScience
{
    internal class ExceptionEqualityComparer : IEqualityComparer<Exception>
    {
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
            return obj.GetHashCode();
        }
    }
}
