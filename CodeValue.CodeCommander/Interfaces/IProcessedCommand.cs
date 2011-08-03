using System;
using System.Reactive;
using ReactiveUI;

namespace CodeValue.CodeCommander.Interfaces
{
    public interface IProcessedCommand : ICommandBase
    {
        string CommandId { get; }
        int SerialNumber { get; }
        int Order { get; }
        string CommandGroup { get; }
        Unit ReturnValue { get; }
        CommandState CurrentState { get; }

        Action<IProcessedCommand> CompleteAction { get; set; }
        Action<IProcessedCommand, Exception> ErrorAction { get; set; }
        Action<IProcessedCommand> FullfillmentAction { get; set; }
        Action<IProcessedCommand> BeforeExecuteAction { get; set; }

        bool ShouldFailIfFiltered { get; }
        TimeSpan? PendingTimeout { get; }
        TimeSpan? ExecutingTimeout { get; }
        bool ShouldFailIfBlocked { get; }
        bool ShouldExecuteForever { get; }

        ReactiveCollection<CommandTrace> CommandTraces { get; }

        IDisposable RegisterForStateChange(IObserver<IObservedChange<CommandBase, CommandState>> observer);
        IDisposable Subscribe(IObserver<ICommandResponse<Unit>> observer);
    }

    public interface IProcessedCommand<T> : IProcessedCommand
    {
        new T ReturnValue { get; }

        IDisposable Subscribe(IObserver<ICommandResponse<T>> observer);
    }
}