using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;

namespace CodeValue.CodeCommander.Interfaces
{
    public interface ICommandProcessor
    {
        IDisposable PublishCommand(CommandBase command, IObserver<ICommandResponse<Unit>> observer = null);
        IDisposable PublishCommand<T>(CommandBase<T> command, IObserver<ICommandResponse<T>> observer = null);
        IDisposable[] PublishOrderedCommands(CommandBase[] commands, IObserver<ICommandResponse<Unit>>[] observers = null);

        
        void CancelCommand(CommandBase command);
        void CancelAllCommand();
        void RerunBlockedCommand(CommandBase command);
        void CancelCommandGroup(string groupId);

        IDisposable RegisterForCompletedCommands(IObserver<CommandBase> observer);
    }
}
