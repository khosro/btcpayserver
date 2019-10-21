using System;
using System.Collections.Generic;
using System.Text;

namespace EntityFramework
{
    public class ConcurrencyException : Exception
    {
        public ConcurrencyException(string message) : base(message)
        { }

        public ConcurrencyException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
