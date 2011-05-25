using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;

namespace CodeValue.CodeCommander.Interfaces
{
    public interface ICommandProcessor
    {
        IObservable<CommandResponse<Unit>> PublishCommand(CommandBase command);
        IObservable<CommandResponse<T>> PublishCommand<T>(CommandBase<T> command);
        void CancelCommand(CommandBase command);
        void RerunBlockedCommand(CommandBase command);

        IDisposable RegisterForCompletedCommands(IObserver<CommandBase> observer);
    }
}
