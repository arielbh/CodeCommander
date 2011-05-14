using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeValue.CodeCommander.Interfaces
{
    public interface ICommandProcessor
    {
        IObservable<Unit> PublishCommand(CommandBase command);
    }
}
