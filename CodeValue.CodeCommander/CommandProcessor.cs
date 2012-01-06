using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using CodeValue.CodeCommander.Exceptions;
using CodeValue.CodeCommander.Interfaces;
using CodeValue.CodeCommander.Ordered;
using ReactiveUI;

namespace CodeValue.CodeCommander
{
    public class CommandProcessor : ICommandProcessor
    {
        private int _commandCounter = 0;
        private readonly IFilterManager _filterManager;
        private readonly object _lockingQueue = new object();
        private ReactiveCollection<CommandBase> _outstandingCommands = new ReactiveCollection<CommandBase>();
        private Dictionary<CommandBase, IDisposable> _subscriptions = new Dictionary<CommandBase, IDisposable>();


        public CommandProcessor(IObservable<ProcessorInput> inputsSource, IFilterManager filterManager)
        {
            _filterManager = filterManager;
            filterManager.ItemsChanged.Subscribe(
                f => TraversePendingCommands());

            _outstandingCommands.ItemsAdded.Subscribe(HandleAddedCommand);

            _outstandingCommands.ItemsRemoved.Subscribe(item =>
            {
                _subscriptions[item].Dispose();
                _subscriptions.Remove(item);
            });
            if (inputsSource != null)
            {
                inputsSource.SelectMany(resp =>
                {
                    foreach (var cmd in _outstandingCommands)
                    {
                        bool result = false;
                        try
                        {
                            result = cmd.InterpretResponse(resp);
                        }
                        catch (Exception ex)
                        {
                            cmd.CompleteCommand(ex);
                        }

                        if (result)
                        {
                            cmd.CurrentState = CommandState.Successed;
                            return Observable.Return(cmd.CurrentState);
                        }
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

        protected virtual void AddCommand(CommandBase command)
        {
            if (command.CurrentState != CommandState.New) throw new CommandProcessorException("Command is not new");
            lock (_lockingQueue)
            {
                _outstandingCommands.Add(command);
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

            CommandState nextState = result
                                         ? CommandState.Executing
                                         : command.ShouldFailIfFiltered ? CommandState.Failed : CommandState.Pending;
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
        public virtual IDisposable PublishCommand<T>(CommandBase<T> command, IObserver<ICommandResponse<T>> observer = null)
        {
            IDisposable subscription = null;
            if (observer != null)
                subscription = command.Subscribe(observer);
            AddCommand(command);
            return subscription;
        }

        public virtual IDisposable[] PublishOrderedCommands(CommandBase[] commands, IObserver<ICommandResponse<Unit>>[] observers = null)
        {
            var disposables = new List<IDisposable>();
            var commandsTracking = commands.ToDictionary(c => c as ICommandBase, c => false);
            var filter = CreateOrderedCommandsFilter(commands, commandsTracking);
            filter.UnregisterToken = RegisterForCompletedCommands(filter.CompletedCommandsObserver);
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
            return _outstandingCommands.ItemsRemoved.Subscribe(observer);
        }
    }




}
