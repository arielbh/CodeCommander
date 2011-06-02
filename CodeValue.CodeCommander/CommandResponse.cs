using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using CodeValue.CodeCommander.Interfaces;

namespace CodeValue.CodeCommander
{
    public class CommandResponse<T>  : ICommandResponse<T>
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
