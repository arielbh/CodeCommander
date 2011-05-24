using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;

namespace CodeValue.CodeCommander
{
    public class CommandResponse<T>
    {
        public CommandResponse(CommandBase sender, T value)
        {
            Sender = sender;
            Value = value;
        }

        public CommandBase Sender { get; private set; }
        public T Value { get; private set; }
    }
}
