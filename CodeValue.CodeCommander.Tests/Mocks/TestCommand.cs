using System;
using CodeValue.CodeCommander.Interfaces;

namespace CodeValue.CodeCommander.Tests.Mocks
{
    public class TestCommand : CommandBase
    {
        private readonly Action<CommandState, Exception> _startRequestAction;
        private readonly bool _blockCanExecute;
        private readonly Func<ProcessorInput, bool> _interpretResponseAction;
        private readonly Action _executeAction;

        public TestCommand(CommandState state, bool shouldFailIfFiltered = false,
                           Action<CommandState, Exception> startRequestAction = null, bool blockCanExecute = false, Func<ProcessorInput, bool> interpretResponseAction = null,
            bool shouldExecuteForever = false, TimeSpan? pendingTimeout = null, TimeSpan? executionTimeout = null,
            Action executeAction = null, bool shouldFailIfBlocked = false, Action<IProcessedCommand> beforeExecuteAction = null, Action<IProcessedCommand, Exception> errorAction = null, 
            Action<IProcessedCommand> fullfillmentAction = null, Action<IProcessedCommand> completeAction = null, bool shouldCompleteAfterExecute = false, string groupId = null)
        {
            _startRequestAction = startRequestAction;
            _blockCanExecute = blockCanExecute;
            _interpretResponseAction = interpretResponseAction;
            _executeAction = executeAction;
            CurrentState = state;
            ShouldFailIfFiltered = shouldFailIfFiltered;
            ShouldExecuteForever = shouldExecuteForever;
            ShouldFailIfBlocked = shouldFailIfBlocked;
            ShouldCompleteAfterExecute = shouldCompleteAfterExecute;
            CommandGroup = groupId;
            PendingTimeout = pendingTimeout;
            ExecutingTimeout = executionTimeout;
            BeforeExecuteAction = beforeExecuteAction;
            CompleteAction = completeAction;
            FullfillmentAction = fullfillmentAction;
            ErrorAction = errorAction;

        }

        public override void StartRequest(CommandState currentState, Exception exception)
        {
            base.StartRequest(currentState, exception);

            if (_startRequestAction != null) _startRequestAction(currentState, exception);
        }

        public override bool InterpretResponse(ProcessorInput response)
        {
            if (_interpretResponseAction != null) return _interpretResponseAction(response);
            return false;
        }

        public override bool CanExecute()
        {
            return !_blockCanExecute;
        }

        public override void Execute()
        {
            if (_executeAction != null) _executeAction();
        }
    }

    public class TestCommand<T> : CommandBase<T>
    {
        private readonly Action<CommandState, Exception> _startRequestAction;
        private readonly bool _blockCanExecute;
        private readonly Func<ProcessorInput, bool> _interpretResponseAction;
        private readonly Action _executeAction;
        private readonly T _returnValue;

        public TestCommand(CommandState state, bool shouldFailIfFiltered = false,
                           Action<CommandState, Exception> startRequestAction = null, bool blockCanExecute = false, Func<ProcessorInput, bool> interpretResponseAction = null,
            bool shouldExecuteForever = false, TimeSpan? pendingTimeout = null, TimeSpan? executionTimeout = null,
            Action executeAction = null, bool shouldFailIfBlocked = false, Action<IProcessedCommand> beforeExecuteAction = null, Action<IProcessedCommand, Exception> errorAction = null,
            Action<IProcessedCommand> fullfillmentAction = null, Action<IProcessedCommand> completeAction = null, bool shouldCompleteAfterExecute = false, T returnValue = default(T))
        {
            _startRequestAction = startRequestAction;
            _blockCanExecute = blockCanExecute;
            _interpretResponseAction = interpretResponseAction;
            _executeAction = executeAction;
            _returnValue = returnValue;
            CurrentState = state;
            ShouldFailIfFiltered = shouldFailIfFiltered;
            ShouldExecuteForever = shouldExecuteForever;
            ShouldFailIfBlocked = shouldFailIfBlocked;
            ShouldCompleteAfterExecute = shouldCompleteAfterExecute;
            PendingTimeout = pendingTimeout;
            ExecutingTimeout = executionTimeout;
            BeforeExecuteAction = beforeExecuteAction;
            CompleteAction = completeAction;
            FullfillmentAction = fullfillmentAction;
            ErrorAction = errorAction;

        }

        public override void StartRequest(CommandState currentState, Exception exception)
        {
            base.StartRequest(currentState, exception);

            if (_startRequestAction != null) _startRequestAction(currentState, exception);
        }

        public override bool InterpretResponse(ProcessorInput response)
        {
            ReturnValue = _returnValue;
            if (_interpretResponseAction != null) return _interpretResponseAction(response);
            return false;
        }

        public override bool CanExecute()
        {
            return !_blockCanExecute;
        }

        public override void Execute()
        {
            if (_executeAction != null) _executeAction();
        }
    }

}