using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using CodeValue.CodeCommander.Exceptions;
using CodeValue.CodeCommander.Interfaces;
using CodeValue.CodeCommander.Ordered;

namespace CodeValue.CodeCommander
{
    public class CommandProcessor : ICommandProcessor
    {
        private int _commandCounter = 0;
        private readonly IFilterManager _filterManager;
        private readonly object _lockingQueue = new object();
        private ObservableCollection<CommandBase> _outstandingCommands = new ObservableCollection<CommandBase>();
        private Dictionary<CommandBase, IDisposable> _subscriptions = new Dictionary<CommandBase, IDisposable>();


        public CommandProcessor(IObservable<ProcessorInput> inputsSource, IFilterManager filterManager)
        {
            //if (useBackgroundDispatcher)
            //{
            //    RxApp.DeferredScheduler = new EventLoopScheduler();
            //}
            _filterManager = filterManager;
            filterManager.ItemsChanged.Subscribe(
                f => TraversePendingCommands());


         //   _outstandingCommands.GetObservableAddedValues().Subscribe(HandleAddedCommand);

            _outstandingCommands.GetObservableRemovedValues().Subscribe(item =>
            {
                _subscriptions[item].Dispose();
                _subscriptions.Remove(item);
            });
            if (inputsSource != null)
            {
                inputsSource.SelectMany(resp =>
                {
                    var failingCommands = new List<Tuple<CommandBase, Exception>>();
                    foreach (var cmd in _outstandingCommands)
                    {
                        bool result = false; 
                        try
                        {                                                                                                                  
                            result = cmd.InterpretResponse(resp);
                        }
                        catch (Exception ex)
                        {
                            failingCommands.Add(new Tuple<CommandBase, Exception>(cmd, ex));
                            
                        }
                        
                        if (result)
                        {
                            cmd.CurrentState = CommandState.Successed;
                            return Observable.Return(cmd.CurrentState);
                        }
                    }

                    foreach (var cmd in failingCommands)
                    {
                        cmd.Item1.CompleteCommand(cmd.Item2);
                    }

                    return Observable.Empty<CommandState>();
                }).Subscribe(newState =>
                {

                });
            }
        }

        protected virtual void TraversePendingCommands()
        {
            _outstandingCommands.Where(c => c.CurrentState == CommandState.Pending).OrderBy(c => c.Order).ToList().ForEach(PushToFilter);
        }

        protected virtual void HandleAddedCommand(CommandBase item)
        {
            item.SerialNumber = _commandCounter++;
            item.CurrentState = CommandState.Pending;
            PushToFilter(item);
            if (_subscriptions.ContainsKey(item))
            {
                _subscriptions[item].Dispose();
            }
            _subscriptions[item] = item.Subscribe(u => { },
                                                  ex =>
                                                  RemoveCommand(item),
                                                  () =>
                                                  RemoveCommand(item));
        }

        protected virtual void HandleAddedCommand<T>(CommandBase<T> item)
        {
            item.SerialNumber = _commandCounter++;
            item.CurrentState = CommandState.Pending;
            PushToFilter(item);
            if (_subscriptions.ContainsKey(item))
            {
                _subscriptions[item].Dispose();
            }
            _subscriptions[item] = item.Subscribe(Observer.Create<ICommandResponse<T>>(
                _ => { },
                ex => { RemoveCommand(item); },
                () => { RemoveCommand(item); }));
        }

        protected virtual void AddCommand(CommandBase command)
        {
            if (command.CurrentState != CommandState.New) throw new CommandProcessorException("Command is not new");
            lock (_lockingQueue)
            {
                _outstandingCommands.Add(command);
                HandleAddedCommand(command);
            }
        }

        protected virtual void AddCommand<T>(CommandBase<T> command)
        {
            if (command.CurrentState != CommandState.New) throw new CommandProcessorException("Command is not new");
            lock (_lockingQueue)
            {
                _outstandingCommands.Add(command);
                HandleAddedCommand<T>(command);
            }
        }
        protected virtual bool RemoveCommand(CommandBase item)
        {
            lock (_lockingQueue)
            {
                return _outstandingCommands
                    .Remove(item);
            }
        }

        protected virtual void PushToFilter(CommandBase command)
        {
            Task.Factory.StartNew(() => DoPushToFilter(command));
        }

        protected void DoPushToFilter(CommandBase command)
        {
            Exception exception = null;
            bool result;
            try
            {
                result = _filterManager.Process(command);
            }
            catch (Exception ex)
            {
                result = false;
                exception = ex;
            }
            CommandState nextState = result ? CommandState.Executing
                                            : command.ShouldFailIfFiltered ? CommandState.Failed
                                                                           : CommandState.Pending;

            command.StartRequest(nextState, exception);
        }

        public virtual IDisposable PublishCommand(CommandBase command, IObserver<ICommandResponse<Unit>> observer = null)
        {
            IDisposable subscription = null;
            if (observer != null)
                subscription = command.Subscribe(observer);
            AddCommand(command);
            return subscription;
        }

        public Task PublishCommand(CommandBase command)
        {
            var source = new TaskCompletionSource<object>();
            PublishCommand(command, Observer.Create<ICommandResponse<Unit>>(
                _ => { }, 
                ex => source.SetException(ex),
                () => source.SetResult(command)));
            return source.Task;
        }

        public virtual IDisposable PublishCommand<T>(CommandBase<T> command, IObserver<ICommandResponse<T>> observer = null)
        {
            IDisposable subscription = null;
            if (observer != null)
                subscription = command.Subscribe(observer);
            AddCommand<T>(command);
            return subscription;
        }

        public Task<IList<T>> PublishCommand<T>(CommandBase<T> command)
        {
            var source = new TaskCompletionSource<IList<T>>();
            var results = new List<T>(); 
            PublishCommand(command, Observer.Create<ICommandResponse<T>>(
                result => results.Add(result.Value),
                ex => source.SetException(ex),
                () => source.SetResult(results)));
            return source.Task;
        }

        public virtual IDisposable[] PublishOrderedCommands(CommandBase[] commands, IObserver<ICommandResponse<Unit>>[] observers = null)
        {
            var disposables = new List<IDisposable>();
            var commandsTracking = commands.OrderBy(c => c.Order).ToDictionary(c => c as ICommandBase, c => false);
            var filter = CreateOrderedCommandsFilter(commands, commandsTracking);
            filter.UnregisterToken =  RegisterForCompletedCommands(filter.CompletedCommandsObserver);
            var pusbFilterDispose =
                RegisterForCompletedCommands(Observer.Create<CommandBase>(c => TraversePendingCommands()));
            filter.Finalizer = () =>
                                   {
                                       pusbFilterDispose.Dispose();
                                       filter.Finalizer = null;
                                       _filterManager.RemoveFilter(filter);
                                   };
            
            _filterManager.AddFilter(filter);
            for (int i = 0; i < commands.Length; i++)
            {
                var command = commands[i];
                IObserver<ICommandResponse<Unit>> observer = null;

                if (observers != null)
                {
                    observer = observers.ElementAtOrDefault(i);
                }
                disposables.Add(PublishCommand(command, observer));
            }
            return disposables.ToArray();
        }

        protected virtual OrderedCommandsFilter CreateOrderedCommandsFilter(CommandBase[] commands, Dictionary<ICommandBase, bool> commandsTracking)
        {
            return new OrderedCommandsFilter(commands, commandsTracking); 
        }


        public virtual void CancelCommand(CommandBase command)
        {
            command.CurrentState = CommandState.Canceled;
            lock (_lockingQueue)
            {
                if (_outstandingCommands.Contains(command))
                {
                    _outstandingCommands.Remove(command);
                }
            }
        }

        public virtual void RerunBlockedCommand(CommandBase command)
        {
            if (command.CurrentState != CommandState.Blocked) throw new CommandProcessorException("Command is not blocked");
            HandleAddedCommand(command);
        }

        public virtual void CancelCommandGroup(string groupId)
        {
            lock (_lockingQueue)
            {
                var commandsToCancel = _outstandingCommands.Where(c => c.CommandGroup == groupId).ToArray();
                foreach (var command in commandsToCancel)
                {
                    CancelCommand(command);
                }
            }
        }

        public virtual IDisposable RegisterForCompletedCommands(IObserver<CommandBase> observer)
        {
            return _outstandingCommands.GetObservableRemovedValues().Subscribe(observer);
        }
    }




}
