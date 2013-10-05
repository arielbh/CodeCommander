using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using CodeValue.CodeCommander.Interfaces;

namespace CodeValue.CodeCommander.Exceptions
{
    [Serializable]
    public class CommandTimeoutException : Exception
    {
        public CommandTimeoutException()
        {

        }

        public CommandTimeoutException(string message) : base(message)
        {
            
        }

        public CommandTimeoutException(string message, CommandState state)
            : base(message)
        {
            State = state;
        }

        public CommandTimeoutException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        public CommandTimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }


        public CommandState State { get; private set; }
    }
}
