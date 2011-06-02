using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;

namespace CodeValue.CodeCommander.Interfaces
{
    public interface ICommandProcessor
    {
        IObservable<ICommandResponse<Unit>> PublishCommand(CommandBase command);
        IObservable<ICommandResponse<T>> PublishCommand<T>(CommandBase<T> command);
        
        void CancelCommand(CommandBase command);
        void RerunBlockedCommand(CommandBase command);
        void CancelCommandGroup(string groupId);

        IDisposable RegisterForCompletedCommands(IObserver<CommandBase> observer);
    }
}
