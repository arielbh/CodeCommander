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
        private int commandCounter = 0;
        private readonly IFilterManager _filterManager;

        //public IObservable<ProcessorInput> CommandResponses { get; private set; }

        public CommandProcessor(IObservable<ProcessorInput> inputsSource, IFilterManager filterManager)
        {
            _filterManager = filterManager;
            filterManager.ItemsChanged.Subscribe(
                o =>
                _outstandingCommands.Where(c => c.CurrentState == CommandState.Pending).ToList().ForEach(
                    PushToFilter));

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
                CurrentState = newState;
            });

           
        }

        private void HandleAddedCommand(CommandBase item)
        {
            item.CommandCounter = commandCounter++;
            item.CurrentState = CommandState.Pending;
            PushToFilter(item);
            if (_subscriptions.ContainsKey(item))
            {
                _subscriptions[item].Dispose();
            }
            _subscriptions[item] = item.Subscribe(u => { },
                                                  ex =>
                                                  _outstandingCommands
                                                      .Remove(item),
                                                  () =>
                                                  _outstandingCommands
                                                      .Remove(item));
        }

        private ReactiveCollection<CommandBase> _outstandingCommands = new ReactiveCollection<CommandBase>();
        Dictionary<CommandBase, IDisposable> _subscriptions = new Dictionary<CommandBase, IDisposable>();

        public CommandState CurrentState { get; private set; }

        public IObservable<CommandResponse<Unit>> PublishCommand(CommandBase command)
        {
            if (command.CurrentState != CommandState.New) throw new Exception("Command is not new");

            _outstandingCommands.Add(command);
            return command;

        }

        public IObservable<CommandResponse<T>> PublishCommand<T>(CommandBase<T> command)
        {
            
            if (command.CurrentState != CommandState.New) throw new Exception("Command is not new");

            _outstandingCommands.Add(command);
            return command;
        }

        public void CancelCommand(CommandBase command)
        {
            command.CurrentState = CommandState.Canceled;
            if (_outstandingCommands.Contains(command))
            {
                _outstandingCommands.Remove(command);
            }
        }

        public void RerunBlockedCommand(CommandBase command)
        {
            if (command.CurrentState != CommandState.Blocked) throw new Exception("Command is not blocked");
            HandleAddedCommand(command);
        }

        public IDisposable RegisterForCompletedCommands(IObserver<CommandBase> observer)
        {
            return _outstandingCommands.ItemsRemoved.Subscribe(observer);
        }

        private void PushToFilter(CommandBase command)
        {
            bool result = _filterManager.Process(command);
            CommandState nextState = result
                                         ? CommandState.Executing
                                         : command.ShouldFailIfFiltered? CommandState.Failed : CommandState.Pending;
            command.StartRequest(nextState);
        }

    
    }



    
}
