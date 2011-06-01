using System;
using System.Reactive;
using ReactiveUI;

namespace CodeValue.CodeCommander.Interfaces
{
    public interface IProcessedCommand
    {
        string CommandId { get; }
        string CommandGroup { get; }
        Unit ReturnValue { get; }

        Action<CommandBase> CompleteAction { get; set; }
        Action<CommandBase, Exception> ErrorAction { get; set; }
        Action<CommandBase> FullfillmentAction { get; set; }
        Action<CommandBase> BeforeExecuteAction { get; set; }

        bool ShouldFailIfFiltered { get;  }
        int? PendingTimeout { get;  }
        int? ExecutingTimeout { get;  }
        bool ShouldExecuteForever { get;  }

        ReactiveCollection<CommandTrace> CommandTraces { get; }
        
        IDisposable RegisterForStateChange(IObserver<IObservedChange<CommandBase, CommandState>> observer);
        //void StartRequest(CommandState currentState);
        //CommandState? InterpretResponse(ProcessorInput response, CommandState currentState);
        //IDisposable Subscribe(IObserver<CommandResponse<Unit>> observer);
    }

    public interface IProcessedCommand<out T> : IProcessedCommand
    {
        new T ReturnValue { get; }
    }
}