using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CodeValue.CodeCommander.Exceptions;
using CodeValue.CodeCommander.Interfaces;
using CodeValue.CodeCommander.Logging;

namespace CodeValue.CodeCommander
{
    public abstract class CommandBase : IObservable<ICommandResponse<Unit>>, IProcessedCommand, INotifyPropertyChanged
    {
#pragma warning disable 649 //RxUI assume private methods are following this convention.
        // ReSharper disable InconsistentNaming
        private CommandState _currentState;
        // ReSharper restore InconsistentNaming
#pragma warning restore  649

        private readonly ReplaySubject<ICommandResponse<Unit>> _inner = new ReplaySubject<ICommandResponse<Unit>>();

        protected CommandBase()
        {
            CommandTraces = new ObservableCollection<CommandTrace>();
            CommandId = Guid.NewGuid().ToString();
            CurrentState = CommandState.New;
            Inner.Subscribe(x => HandleFullfillment(), HandleError, HandleCompletion);
            //this.ObservableForProperty(c => c.CurrentState).Subscribe(HandleStateChange);
        }

        public IDisposable RegisterInputSource(IObservable<ProcessorInput> inputsSource)
        {
            return inputsSource.Subscribe(Observer.Create<ProcessorInput>(input =>
            {
                try
                {
                    var result = InterpretResponse(input);
                    if (result)
                    {
                        CurrentState = CommandState.Successed;
                    }
                }
                catch (Exception ex)
                {
                    CompleteCommand(ex);
                }
            }));

        }

        private void HandleStateChange(CommandState newState)
        {
            Task.Factory.StartNew(() => AsyncHandleStateChange(newState));
            if (newState == CommandState.Successed)
            {
                SignalCommandFulfillment();
                if (!ShouldExecuteForever)
                {
                    CompleteCommand();
                }
                else
                {
                    CurrentState = CommandState.Executing;
                }
            }
            if (newState == CommandState.Canceled)
            {
                CompleteCommand();
            }

            RegisterTimers(newState);
        }

        private void AsyncHandleStateChange(CommandState state)
        {
            CommandTraces.Add(new CommandTrace { DateTime = DateTime.Now, State = state });
            LoggerFacade.Log(string.Format("{3} Command {2} has changed its state. Id: {0} Current State: {1}", CommandId, state, GetType().Name, DateTime.Now.ToString("h:mm:ss.ffff")), Category.Debug, Priority.Low);
        }

        private void RegisterTimers(CommandState currentState)
        {
            if (currentState == CommandState.Pending && PendingTimeout.HasValue)
            {
                Observable.Timer(PendingTimeout.Value).Subscribe(
                    a =>
                    {
                        if (CurrentState == CommandState.Pending)
                        {
                            CompleteCommand(
                                new CommandTimeoutException("Command has exceded its Pending timeout", CommandState.Pending));

                        }
                    });
            }
            if (currentState == CommandState.Executing && ExecutingTimeout.HasValue)
            {
                Observable.Timer(ExecutingTimeout.Value).Subscribe(
                    a =>
                    {
                        if (CurrentState == CommandState.Executing)
                        {
                            CompleteCommand(
                                new CommandTimeoutException("Command has exceded its Executing timeout", CommandState.Executing));

                        }
                    });
            }
        }

        protected virtual void SignalCommandFulfillment()
        {
            LoggerFacade.Log(string.Format("{3} Command {2} is singaling for fullfilment. Id: {0} Current State: {1}", CommandId, CurrentState, GetType().Name, DateTime.Now.ToString("h:mm:ss.ffff")), Category.Debug, Priority.Low);

            Inner.OnNext(new CommandResponse<Unit>(this, new Unit()));
        }

        internal protected virtual void CompleteCommand(Exception ex = null)
        {
            LoggerFacade.Log(string.Format("{3} Command {2} is completing. Id: {0} Current State: {1}", CommandId, CurrentState, GetType().Name, DateTime.Now.ToString("h:mm:ss.ffff")), Category.Debug, Priority.Low);

            if (ex != null)
            {
                LoggerFacade.Log(string.Format("{4} Command {3} has failed. Id: {0} Current State: {1} Exception: {2}", CommandId, CurrentState, ex, GetType().Name, DateTime.Now.ToString("h:mm:ss.ffff")), Category.Debug, Priority.Low);
                CurrentState = CommandState.Failed;
                Inner.OnError(ex);
            }
            else
            {
                LoggerFacade.Log(string.Format("{3} Command {2} has completed. Id: {0} Current State: {1}", CommandId, CurrentState, GetType().Name, DateTime.Now.ToString("h:mm:ss.ffff")), Category.Debug, Priority.Low);
                Inner.OnCompleted();
            }
        }

        protected virtual void HandleFullfillment()
        {
            if (FullfillmentAction != null)
            {
                FullfillmentAction(this);
            }

        }

        protected virtual void HandleError(Exception ex)
        {
            if (ErrorAction != null)
            {
                ErrorAction(this, ex);
            }
        }

        protected virtual void HandleCompletion()
        {
            if (CompleteAction != null)
            {
                CompleteAction(this);
            }

        }

        protected ReplaySubject<ICommandResponse<Unit>> Inner { get { return _inner; } }

        public IDisposable Subscribe(IObserver<ICommandResponse<Unit>> observer)
        {
            return Inner.Subscribe(observer);
        }

        public virtual void StartRequest(CommandState currentState, Exception exception)
        {
            CurrentState = currentState;

            if (currentState == CommandState.Executing)
            {
                SignalCommandCanStartExecuting();
                return;
            }
            if (CurrentState == CommandState.Failed)
            {
                if (exception != null)
                {
                    CompleteCommand(exception);
                }
                else
                {
                    CompleteCommand(new CommandFailureException("Command can not be started. Most likely due to filters"));
                    
                }
            }
            // This method is called at the start of every request - this method
            // should do the following:
            //
            //      * If the request is invalid (and will never be valid) due to the
            //        current state or for other reasons, it should throw an Exception.
            //
            //      * If the request can be issued immediately, it should be issued
            //        and the signalCommandIsInitiated() method should be called.
            //
            //      * If the command cannot be issued at this time but should be
            //        queued, do nothing.
        }

        public abstract bool InterpretResponse(ProcessorInput response);
        // The contract of this command is that it should return:
        //
        //      * null if it is not interested in the command, or if command
        //        processing should continue.
        //
        //      * The new (or current) Robot State if this command handles
        //        the response completely and no further processing should
        //        happen.
        // 
        // Note that if this response completes the command, the RobotCommand
        // itself should call completeCommand().
        //
        // If this response allows a previously queued command to now be 
        // issued (i.e. we were previously in the "NotLoggedIn" state and
        // now we are in the "Command Ready" state), the
        // signalCommandIsInitiated method should be called after the command is
        // sent over the wire. 

        internal virtual void SignalCommandCanStartExecuting()
        {
            try
            {
                LoggerFacade.Log(string.Format("{3} Command {2} is starting to execute. Id: {0} Current State: {1}", CommandId, CurrentState, GetType().Name, DateTime.Now.ToString("h:mm:ss.ffff")), Category.Debug, Priority.Low);

                if (CanExecute())
                {
                    if (BeforeExecuteAction != null)
                    {
                        BeforeExecuteAction(this);
                    }
                    LoggerFacade.Log(string.Format("{3} Command {2} is Executing. Id: {0} Current State: {1}", CommandId, CurrentState, GetType().Name, DateTime.Now.ToString("h:mm:ss.ffff")), Category.Debug, Priority.Low);
                    Execute();
                    if (ShouldCompleteAfterExecute)
                    {
                        CurrentState = CommandState.Successed;
                    }
                }
                else
                {
                    LoggerFacade.Log(string.Format("{3} Command {2} returned False for CanExecute. Id: {0} Current State: {1}", CommandId, CurrentState, GetType().Name, DateTime.Now.ToString("h:mm:ss.ffff")), Category.Debug, Priority.Low);

                    if (ShouldFailIfBlocked)
                    {
                        CompleteCommand(new CommandFailureException("Command was supposed to be blocked, due to configuration command has failed."));
                        return;
                    }
                    CurrentState = CommandState.Blocked;
                }
            }
            catch (Exception ex)
            {
                CompleteCommand(ex);
            }
        }

        public override string ToString()
        {
            return string.Format("Type: {0} Command: {1}", GetType().Name, CommandId);
        }

        public CommandState CurrentState
        {
            get { return _currentState; }
            internal protected set
            {
                if (value != _currentState)
                {
                    _currentState = value;
                    OnPropertyChanged("CurrentState");
                    HandleStateChange(CurrentState);
                }
            }
        }

        public abstract bool CanExecute();
        public abstract void Execute();

        public string CommandId { get; private set; }

#pragma warning disable 649 //RxUI assume private methods are following this convention.
        // ReSharper disable InconsistentNaming
        private Unit _returnValue;
        // ReSharper restore InconsistentNaming
#pragma warning restore  649

        public Unit ReturnValue
        {
            get { return _returnValue; }
            protected set
            {
                if (value != _returnValue)
                {
                    _returnValue = value;
                    OnPropertyChanged("ReturnValue");
                }
            }
        }

        public Action<IProcessedCommand> CompleteAction { get; set; }
        public Action<IProcessedCommand, Exception> ErrorAction { get; set; }
        public Action<IProcessedCommand> FullfillmentAction { get; set; }
        public Action<IProcessedCommand> BeforeExecuteAction { get; set; }
        public ObservableCollection<CommandTrace> CommandTraces { get; private set; }
        public bool ShouldFailIfFiltered { get; protected set; }
        public TimeSpan? PendingTimeout { get; protected set; }
        public TimeSpan? ExecutingTimeout { get; protected set; }
        public bool ShouldExecuteForever { get; protected set; }
        public bool ShouldFailIfBlocked { get; protected set; }
        public bool ShouldCompleteAfterExecute { get; protected set; }
        public string CommandGroup { get; protected set; }
        public int SerialNumber { get; internal set; }
        private int? _order;
        public int Order
        {
            get
            {
                if (!_order.HasValue)
                {
                    return SerialNumber;
                }
                return _order.Value;
            }
            set { _order = value; }
        }

        private ILoggerFacade _loggerFacade;
        internal ILoggerFacade LoggerFacade
        {
            get { return _loggerFacade ?? new TraceLogger(); }
            set { _loggerFacade = value; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public abstract class CommandBase<T> : CommandBase, IObservable<ICommandResponse<T>>, IProcessedCommand<T>
    {
        public CommandBase()
        {
            Inner.Subscribe(x => HandleFullfillment(), HandleError, HandleCompletion);
        }

        private readonly ReplaySubject<ICommandResponse<T>> _inner = new ReplaySubject<ICommandResponse<T>>();

        public IDisposable Subscribe(IObserver<ICommandResponse<T>> observer)
        {
            return Inner.Subscribe(observer);
        }

        protected new ReplaySubject<ICommandResponse<T>> Inner { get { return _inner; } }

        protected override void SignalCommandFulfillment()
        {
            Inner.OnNext(new CommandResponse<T>(this, ReturnValue));
        }



#pragma warning disable 649 //RxUI assume private methods are following this convention.
        // ReSharper disable InconsistentNaming
        private T _returnValue;
        // ReSharper restore InconsistentNaming
#pragma warning restore  649

        public new T ReturnValue
        {
            get { return _returnValue; }
            protected set
            {

                _returnValue = value;
                OnPropertyChanged("ReturnValue");
            }
        }



    }

    public class CommandTrace
    {
        public DateTime DateTime { get; set; }
        public CommandState State { get; set; }
    }
}
