using System;
using System.Reactive;
using ReactiveUI;

namespace CodeValue.CodeCommander.Interfaces
{
    public interface IProcessedCommand : ICommandBase
    {
        string CommandId { get; }
        int SerialNumber { get; }
        string CommandGroup { get; }
        Unit ReturnValue { get; }
        CommandState CurrentState { get; }

        Action<IProcessedCommand> CompleteAction { get; set; }
        Action<IProcessedCommand, Exception> ErrorAction { get; set; }
        Action<IProcessedCommand> FullfillmentAction { get; set; }
        Action<IProcessedCommand> BeforeExecuteAction { get; set; }

        bool ShouldFailIfFiltered { get; }
        int? PendingTimeout { get; }
        int? ExecutingTimeout { get; }
        bool ShouldExecuteForever { get; }

        ReactiveCollection<CommandTrace> CommandTraces { get; }

        IDisposable RegisterForStateChange(IObserver<IObservedChange<CommandBase, CommandState>> observer);
        IDisposable Subscribe(IObserver<ICommandResponse<Unit>> observer);
        //void StartRequest(CommandState currentState);
        //CommandState? InterpretResponse(ProcessorInput response, CommandState currentState);
        //IDisposable Subscribe(IObserver<CommandResponse<Unit>> observer);
    }

    public interface IProcessedCommand<T> : IProcessedCommand
    {
        new T ReturnValue { get; }

        IDisposable Subscribe(IObserver<ICommandResponse<T>> observer);
    }
}