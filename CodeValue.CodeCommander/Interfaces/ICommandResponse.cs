using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeValue.CodeCommander.Interfaces
{
    public interface ICommandResponse<T>
    {
        CommandBase Sender { get; }
        T Value { get;  }
    }
}
