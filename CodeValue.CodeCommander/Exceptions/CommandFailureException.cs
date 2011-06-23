using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace CodeValue.CodeCommander.Exceptions
{
    [Serializable]
    public class CommandFailureException : Exception
    {
        public CommandFailureException() { }
        public CommandFailureException(string message) : base(message) { }
        public CommandFailureException(string message, Exception inner) : base(message, inner) { }
        protected CommandFailureException(
          SerializationInfo info,
          StreamingContext context)
            : base(info, context) { }
    }

}
