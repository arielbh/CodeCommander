using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using CodeValue.CodeCommander.Interfaces;


namespace CodeValue.CodeCommander.Ordered
{
    public class OrderedCommandsFilter : FilterBase, IDisposable
    {
        private CommandBase[] _commands;
        private Dictionary<ICommandBase, bool> _commandsTracking;

        public OrderedCommandsFilter(CommandBase[] commands, Dictionary<ICommandBase, bool> commandsTracking)
        {
            _commands = commands;
            _commandsTracking = commandsTracking;
            CompletedCommandsObserver = Observer.Create<CommandBase>(c =>
            {
                if (c.CurrentState != CommandState.Successed) return;
                if (commandsTracking.ContainsKey(c))
                {
                    commandsTracking[c] = true;
                }
                if (commandsTracking.Values.All(v => v)) Dispose();
            });
        }

        public Action Finalizer { get; set; }

        public IObserver<CommandBase> CompletedCommandsObserver { get; set; }

        public IDisposable UnregisterToken { get; set; }

        public override bool Process(ICommandBase command)
        {

            if (_commandsTracking.ContainsKey(command))
            {
                var index = Array.IndexOf(_commands, command);
                if (index == 0) // First Command, no special requirments
                {
                    return true;
                }
                var prevCommand = _commands[index-1];
                if (_commandsTracking.ContainsKey(prevCommand))
                {
                    return  _commandsTracking[prevCommand];
                }
                return false;
            }
            return true;
        }

        public void Dispose()
        {
            UnregisterToken.Dispose();
            _commands = null;
            _commandsTracking = null;
            if (Finalizer != null)
            {
                Finalizer();
            }
        }
    }
}