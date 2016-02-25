using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace CodeValue.CodeCommander.Interfaces
{
    public interface ICommandProcessor
    {
        IDisposable PublishCommand(CommandBase command, IObserver<ICommandResponse<Unit>> observer = null);
        IDisposable PublishCommand<T>(CommandBase<T> command, IObserver<ICommandResponse<T>> observer = null);
        IDisposable[] PublishOrderedCommands(CommandBase[] commands, IObserver<ICommandResponse<Unit>>[] observers = null);

        Task PublishCommand(CommandBase command);
        Task<IList<T>>  PublishCommand<T>(CommandBase<T> command);

        void CancelCommand(CommandBase command);
        void RerunBlockedCommand(CommandBase command);
        void CancelCommandGroup(string groupId);

        IDisposable RegisterForCompletedCommands(IObserver<CommandBase> observer);
    }
}
