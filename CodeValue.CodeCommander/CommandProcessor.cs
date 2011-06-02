using System;
using System.Collections.Concurrent;
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
                _outstandingCommands.Where(c => c.CurrentState == CommandState.Pending).ToList().ForEach(PushToFilter));

            _outstandingCommands.ItemsAdded.Subscribe(HandleAddedCommand);

            _outstandingCommands.ItemsRemoved.Subscribe(item =>
            {
                _subscriptions[item].Dispose();
                _subscriptions.Remove(item);
            });

            inputsSource.SelectMany(resp =>
            {
                foreach (var cmd in _outstandingCommands)
                {
                    var result = cmd.InterpretResponse(resp, cmd.CurrentState);
                    if (result != null)
                    {
                        cmd.CurrentState = result.Value;
                        return Observable.Return(result.Value);
                    }
                }

                return Observable.Empty<CommandState>();
            }).Subscribe(newState =>
            {
                
            });
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
            if (command.CurrentState != CommandState.New) throw new Exception("Command is not new");
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
            bool result = _filterManager.Process(command);
            CommandState nextState = result
                                         ? CommandState.Executing
                                         : command.ShouldFailIfFiltered ? CommandState.Failed : CommandState.Pending;
            command.StartRequest(nextState);
        }

        public IObservable<ICommandResponse<Unit>> PublishCommand(CommandBase command)
        {
            AddCommand(command);
            return command;

        }

        public IObservable<ICommandResponse<T>> PublishCommand<T>(CommandBase<T> command)
        {
            AddCommand(command);
            return command;
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
            if (command.CurrentState != CommandState.Blocked) throw new Exception("Command is not blocked");
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
