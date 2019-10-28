using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetCore
{
    public class ModelStateValidationException : Exception
    {
        public List<string> Messages { get; }

        public ModelStateValidationException(List<string> messages) : base(string.Join(",", messages))
        {
            Messages = messages;
        }

        public ModelStateValidationException(string message) : base(message)
        { }

        public ModelStateValidationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
