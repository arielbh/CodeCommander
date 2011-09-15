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
                o =>
                _outstandingCommands.Where(c => c.CurrentState == CommandState.Pending).OrderBy(c => c.Order).ToList().ForEach(PushToFilter));

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
                            result = cmd.InterpretResponse(resp, cmd.CurrentState);
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

        private void HandleAddedCommand(CommandBase item)
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

        private void AddCommand(CommandBase command)
        {
            if (command.CurrentState != CommandState.New) throw new CommandProcessorException("Command is not new");
            lock (_lockingQueue)
            {
                _outstandingCommands.Add(command);
            }
        }
        private bool RemoveCommand(CommandBase item)
        {
            lock (_lockingQueue)
            {
                return _outstandingCommands
                    .Remove(item);
            }
        }

        private void PushToFilter(CommandBase command)
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

        public IDisposable PublishCommand(CommandBase command, IObserver<ICommandResponse<Unit>> observer = null)
        {
            IDisposable subscription = null;
            if (observer != null)
                subscription = command.Subscribe(observer);
            AddCommand(command);
            return subscription;


        }
        public IDisposable PublishCommand<T>(CommandBase<T> command, IObserver<ICommandResponse<T>> observer = null)
        {
            IDisposable subscription = null;
            if (observer != null)
                subscription = command.Subscribe(observer);
            AddCommand(command);
            return subscription;
        }


        public void CancelCommand(CommandBase command)
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

        public void RerunBlockedCommand(CommandBase command)
        {
            if (command.CurrentState != CommandState.Blocked) throw new CommandProcessorException("Command is not blocked");
            HandleAddedCommand(command);
        }

        public void CancelCommandGroup(string groupId)
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

        public IDisposable RegisterForCompletedCommands(IObserver<CommandBase> observer)
        {
            return _outstandingCommands.ItemsRemoved.Subscribe(observer);
        }
    }




}
