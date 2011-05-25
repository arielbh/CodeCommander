﻿using System;
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
    public abstract class CommandBase : ReactiveObject, IObservable<CommandResponse<Unit>>, ICommandBase
    {
        internal int CommandCounter { get; set; }
        private readonly ReplaySubject<CommandResponse<Unit>> _inner = new ReplaySubject<CommandResponse<Unit>>();

        protected CommandBase()
        {
            CommandId = Guid.NewGuid().ToString();
            CurrentState = CommandState.New;
            HasBeenIssued = false;
            Inner.Subscribe(HandleFullfillment, HandleError, HandleCompletion);
            this.ObservableForProperty(c => c.CurrentState).Subscribe(b =>
                                  {
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

                                      if (b.Value == CommandState.Pending && PendingTimeout.HasValue)
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
                                      if (b.Value == CommandState.Executing && ExecutingTimeout.HasValue)
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

                                  });

        }

        public bool HasBeenIssued { get; private set; }
        private CommandState _CurrentState;
        public CommandState CurrentState
        {
            get { return _CurrentState; }
            set { this.RaiseAndSetIfChanged(x => x.CurrentState, value); }
        }

        protected ReplaySubject<CommandResponse<Unit>> Inner { get { return _inner; } }

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

        internal void SignalCommandCanStartExecuting()
        {
            HasBeenIssued = true;
            if (CanExecute())
            {
                Execute();
            }
            else
            {
                CurrentState = CommandState.Blocked;
            }
        }
        
        protected virtual void SignalCommandFulfillment()
        {
            Inner.OnNext(new CommandResponse<Unit>(this, new Unit()));
        }

        protected void CompleteCommand(Exception ex = null)
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

        
        public abstract bool CanExecute();
        public abstract void Execute();

        public IDisposable Subscribe(IObserver<CommandResponse<Unit>> observer)
        {
            return Inner.Subscribe(observer);
        }

        public bool ShouldFailIfFiltered { get; protected set; }
        public int? PendingTimeout { get; protected set; }
        public int? ExecutingTimeout { get; protected set; }
        public bool ShouldExecuteForever { get; protected set; }

        public IDisposable RegisterForStateChange(IObserver<IObservedChange<CommandBase, CommandState>> observer)
        {
            return this.ObservableForProperty(c => c.CurrentState).Subscribe(observer);
        }
        protected virtual void HandleFullfillment(CommandResponse<Unit> commandResponse)
        {
            
        }

        protected virtual  void HandleError(Exception ex)
        {
            
        }

        protected virtual void HandleCompletion()
        {

        }

        public string CommandId { get; private set; }

        public override string ToString()
        {
            return "Command: " + CommandId;
        }

        public Unit ReturnValue { get; protected set; }
    }

    public abstract class CommandBase<T> : CommandBase, IObservable<CommandResponse<T>>
    {
        public CommandBase()
        {
            Inner.Subscribe(HandleFullfillment, HandleError, HandleCompletion);
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

        protected new virtual void HandleFullfillment(CommandResponse<T> commandResponse)
        {
        }


    }
}