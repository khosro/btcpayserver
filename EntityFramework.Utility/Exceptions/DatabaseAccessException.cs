using System;
using System.Collections.Generic;
using System.Text;

namespace EntityFramework
{
    public class DatabaseAccessException : Exception
    {
        public DatabaseAccessException(string message) : base(message)
        { }

        public DatabaseAccessException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
