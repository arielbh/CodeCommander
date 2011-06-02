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
    public abstract class CommandBase : ReactiveObject, IObservable<ICommandResponse<Unit>>, ICommandBase, IProcessedCommand
    {
#pragma warning disable 649 //RxUI assume private methods are following this convention.
// ReSharper disable InconsistentNaming
        private CommandState _CurrentState;
// ReSharper restore InconsistentNaming
#pragma warning restore  649

        private readonly ReplaySubject<ICommandResponse<Unit>> _inner = new ReplaySubject<ICommandResponse<Unit>>();

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
                CurrentState = CommandState.Failed;
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

        protected ReplaySubject<ICommandResponse<Unit>> Inner { get { return _inner; } }

        public IDisposable RegisterForStateChange(IObserver<IObservedChange<CommandBase, CommandState>> observer)
        {
            return this.ObservableForProperty(c => c.CurrentState).Subscribe(observer);
        }

        public IDisposable Subscribe(IObserver<ICommandResponse<Unit>> observer)
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
                if (BeforeExecuteAction != null)
                {
                    BeforeExecuteAction(this);
                }
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

#pragma warning disable 649 //RxUI assume private methods are following this convention.
        // ReSharper disable InconsistentNaming
        private Unit _ReturnValue;
        // ReSharper restore InconsistentNaming
#pragma warning restore  649

        public Unit ReturnValue
        {
            get { return _ReturnValue; }
            protected set { this.RaiseAndSetIfChanged(x => x.ReturnValue, value); }
        }

        public Action<IProcessedCommand> CompleteAction { get; set; }
        public Action<IProcessedCommand, Exception> ErrorAction { get; set; }
        public Action<IProcessedCommand> FullfillmentAction { get; set; }
        public Action<IProcessedCommand> BeforeExecuteAction { get; set; }
        public ReactiveCollection<CommandTrace> CommandTraces { get; private set; }
        public bool ShouldFailIfFiltered { get; protected set; }
        public int? PendingTimeout { get; protected set; }
        public int? ExecutingTimeout { get; protected set; }
        public bool ShouldExecuteForever { get; protected set; }
        public string CommandGroup { get; protected set; }
        public int SerialNumber { get; internal set; }
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
         protected override void  HandleError(Exception ex)
        {
            if (ErrorAction != null)
            {
                ErrorAction(this, ex);
            }   
        }

        protected override void HandleCompletion()
        {
            if (CompleteAction != null)
            {
                CompleteAction(this);
            }

        }


#pragma warning disable 649 //RxUI assume private methods are following this convention.
        // ReSharper disable InconsistentNaming
        private T _ReturnValue;
        // ReSharper restore InconsistentNaming
#pragma warning restore  649
        
        public new T ReturnValue
        {
            get { return _ReturnValue; }
            protected set { this.RaiseAndSetIfChanged(x => x.ReturnValue, value); }
        }

        public new Action<IProcessedCommand<T>> CompleteAction { get; set; }
        public new Action<IProcessedCommand<T>> BeforeExecuteAction { get; set; }
        public new Action<IProcessedCommand<T>> FullfillmentAction { get; set; }
        public new Action<IProcessedCommand<T>, Exception> ErrorAction { get; set; }
    }

    public class CommandTrace
    {
        public DateTime DateTime { get; set; }
        public CommandState State { get; set; }
    }
}
