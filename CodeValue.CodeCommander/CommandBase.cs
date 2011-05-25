using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using CodeValue.CodeCommander.Interfaces;
using ReactiveUI;

namespace CodeValue.CodeCommander
{
    public abstract class CommandBase : ReactiveObject, IObservable<CommandResponse<Unit>>, ICommandBase, IProcessedCommand
    {
        private CommandState _CurrentState;
        internal int CommandCounter { get; set; }
        private readonly ReplaySubject<CommandResponse<Unit>> _inner = new ReplaySubject<CommandResponse<Unit>>();

        protected CommandBase()
        {
            CommandTraces = new ReactiveCollection<CommandTrace>();
            CommandId = Guid.NewGuid().ToString();
            CurrentState = CommandState.New;
            Inner.Subscribe(x => HandleFullfillment(), HandleError, HandleCompletion);
            this.ObservableForProperty(c => c.CurrentState).Subscribe(HandleStateChange);
        }

        private void HandleStateChange(IObservedChange<CommandBase, CommandState> b)
        {
            CommandTraces.Add(new CommandTrace {DateTime = DateTime.Now, State = b.Value});
            if (b.Value == CommandState.Successed)
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

            RegisterTimers(b.Value);
        }

        private void RegisterTimers(CommandState currentState)
        {
            if (currentState == CommandState.Pending && PendingTimeout.HasValue)
            {
                Observable.Timer(new TimeSpan(0, 0, 0, 0, PendingTimeout.Value)).Subscribe(
                    a =>
                        {
                            if (CurrentState == CommandState.Pending)
                            {
                                CompleteCommand(
                                    new Exception("Command has exceded its Pending timeout"));

                            }                                                     
                        });
            }
            if (currentState == CommandState.Executing && ExecutingTimeout.HasValue)
            {
                Observable.Timer(new TimeSpan(0, 0, 0, 0, ExecutingTimeout.Value)).Subscribe(
                    a =>
                        {
                            if (CurrentState == CommandState.Executing)
                            {
                                CompleteCommand(
                                    new Exception("Command has exceded its Executing timeout"));

                            }
                        });
            }
        }
          
        protected virtual void SignalCommandFulfillment()
        {
            Inner.OnNext(new CommandResponse<Unit>(this, new Unit()));
        }

        protected virtual void CompleteCommand(Exception ex = null)
        {
            if (ex != null)
            {
                Inner.OnError(ex);
            }
            else
            {
                
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

        protected virtual  void HandleError(Exception ex)
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

        protected ReplaySubject<CommandResponse<Unit>> Inner { get { return _inner; } }

        public IDisposable RegisterForStateChange(IObserver<IObservedChange<CommandBase, CommandState>> observer)
        {
            return this.ObservableForProperty(c => c.CurrentState).Subscribe(observer);
        }

        public IDisposable Subscribe(IObserver<CommandResponse<Unit>> observer)
        {
            return Inner.Subscribe(observer);
        }

        public virtual void StartRequest(CommandState currentState)
        {
            CurrentState = currentState;
            if (currentState == CommandState.Executing)
            {
                SignalCommandCanStartExecuting();
                return;
            }
            if (CurrentState == CommandState.Failed)
            {
                CompleteCommand(new Exception("Command can not be started. Most likely due to filers"));
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

        public abstract CommandState? InterpretResponse(ProcessorInput response, CommandState currentState);
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
            if (CanExecute())
            {
                Execute();
            }
            else
            {
                CurrentState = CommandState.Blocked;
            }
        }

        public override string ToString()
        {
            return "Command: " + CommandId;
        }

        public CommandState CurrentState
        {
            get { return _CurrentState; }
            set { this.RaiseAndSetIfChanged(x => x.CurrentState, value); }
        }

        public abstract bool CanExecute();
        public abstract void Execute();
        
        public string CommandId { get; private set; }
        public Unit ReturnValue { get; protected set; }
        public Action<CommandBase> CompleteAction { get; set; }
        public Action<CommandBase, Exception> ErrorAction { get; set; }
        public Action<CommandBase> FullfillmentAction { get; set; }
        public ReactiveCollection<CommandTrace> CommandTraces { get; private set; }
        public bool ShouldFailIfFiltered { get; protected set; }
        public int? PendingTimeout { get; protected set; }
        public int? ExecutingTimeout { get; protected set; }
        public bool ShouldExecuteForever { get; protected set; }
        public string CommandGroup { get; protected set; }
    }

    public abstract class CommandBase<T> : CommandBase, IObservable<CommandResponse<T>>, IProcessedCommand<T>
    {
        public CommandBase()
        {
            Inner.Subscribe(x => HandleFullfillment(), HandleError, HandleCompletion);
        }

        private readonly ReplaySubject<CommandResponse<T>>  _inner = new ReplaySubject<CommandResponse<T>>();

        public IDisposable Subscribe(IObserver<CommandResponse<T>> observer)
        {
           return Inner.Subscribe(observer);
        }

        protected new ReplaySubject<CommandResponse<T>> Inner { get { return _inner; } }

        protected override void SignalCommandFulfillment()
        {
            Inner.OnNext(new CommandResponse<T>(this, ReturnValue));
        }

        public new T ReturnValue { get; protected set; }
    }

    public class CommandTrace
    {
        public DateTime DateTime { get; set; }
        public CommandState State { get; set; }
    }
}
