using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Concurrency;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using CodeValue.CodeCommander.Interfaces;
using ReactiveUI;

namespace CodeValue.CodeCommander
{
    public class CommandProcessor : ICommandProcessor
    {

        public IObservable<CommandResponse> RobotResponses { get; private set; }

        public CommandProcessor(IObservable<CommandResponse> robotResponses)
        {
            _outstandingCommands.ItemsAdded.Subscribe(item =>
                _subscriptions[item] = item.Subscribe((u) => { },
                    ex => _outstandingCommands.Remove(item),
                    () => _outstandingCommands.Remove(item)));

            _outstandingCommands.ItemsRemoved.Subscribe(item =>
            {
                _subscriptions[item].Dispose();
                _subscriptions.Remove(item);
            });

            robotResponses.SelectMany(resp =>
            {
                foreach (var cmd in _outstandingCommands)
                {
                    var result = cmd.InterpretResponse(resp, CurrentState);
                    if (result != null)
                    {
                        return Observable.Return(result.Value);
                    }
                }

                return Observable.Empty<CommandState>();
            }).Subscribe(newState =>
            {
                CurrentState = newState;
            });
        }

        private ReactiveCollection<CommandBase> _outstandingCommands = new ReactiveCollection<CommandBase>();
        Dictionary<CommandBase, IDisposable> _subscriptions = new Dictionary<CommandBase, IDisposable>();

        public CommandState CurrentState { get; private set; }

        public IObservable<Unit> PublishCommand(CommandBase command)
        {
            _outstandingCommands.Add(command);
            command.StartRequest(CommandState.Pending);
            return command;
        }


    }



    
}
