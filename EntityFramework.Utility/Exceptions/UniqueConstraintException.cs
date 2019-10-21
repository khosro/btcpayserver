using System;
using System.Collections.Generic;
using System.Text;

namespace EntityFramework
{
    public class UniqueConstraintException : Exception
    {
        public UniqueConstraintException(string message) : base(message)
        { }

        public UniqueConstraintException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
