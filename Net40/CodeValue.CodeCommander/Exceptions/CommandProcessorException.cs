using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeValue.CodeCommander.Exceptions
{
    [Serializable]
    public class CommandProcessorException : Exception
    {
        public CommandProcessorException() { }
        public CommandProcessorException(string message) : base(message) { }
        public CommandProcessorException(string message, Exception inner) : base(message, inner) { }
        protected CommandProcessorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
