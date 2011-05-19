using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using CodeValue.CodeCommander.Interfaces;

namespace CodeValue.CodeCommander
{
    public abstract class CommandBase : IObservable<Unit>, ICommandBase
    {
        private readonly ReplaySubject<Unit> _inner = new ReplaySubject<Unit>();


        protected CommandBase()
        {
            CurrentState = CommandState.Pending;
            HasBeenIssued = false;
            Inner.Subscribe(_ =>
                                 {
                                     HasBeenIssued = true;
                                     if (CanExecute())
                                     {
                                         Execute();
                                     }
                                 });
            
        }

        public bool HasBeenIssued { get; private set; }
        public CommandState CurrentState { get; set; }

        internal ReplaySubject<Unit> Inner
        {
            get { return _inner; }
        }

        public virtual void StartRequest(CommandState currentState)
        {
            CurrentState = currentState;
            if (currentState == CommandState.Executing)
            {
                SignalCommandIsInitiated();
                return;
            }
            if (CurrentState == CommandState.Failed)
            {
                throw new Exception("Command can not be started. Most likely due to filers");
                return;
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

        public abstract CommandState? InterpretResponse(CommandResponse response, CommandState currentState);
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


        protected void SignalCommandIsInitiated()
        {
            Inner.OnNext(new Unit());
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
        public IDisposable Subscribe(IObserver<Unit> observer)
        {
            return Inner.Subscribe(observer);
        }

        public bool ShouldFailIfFiltered { get; set; }


    }
}
