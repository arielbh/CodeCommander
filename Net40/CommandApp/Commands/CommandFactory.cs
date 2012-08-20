using System;
using System.Reactive;
using CodeValue.CodeCommander;
using CodeValue.CodeCommander.Interfaces;
using ReactiveUI;

namespace CommandApp.Commands
{
    public class CommandFactory
    {
        private readonly IFilterManager _filterManager;
        private BusyFilter _busyFilter;
        public CommandFactory(IFilterManager filterManager)
        {
            _filterManager = filterManager;
            _busyFilter = new BusyFilter();
        }
        public CommandBase CreateCommand(Type type, params object[] args)
        {
            CommandBase instance = (CommandBase)Activator.CreateInstance(type, args);

            if (instance is BusyCommandBase)                
            {
                instance.BeforeExecuteAction = c => _filterManager.AddFilter(_busyFilter);
                instance.CompleteAction = c => _filterManager.RemoveFilter(_busyFilter);
                instance.RegisterForStateChange(Observer.Create<IObservedChange<CommandBase, CommandState>>(b =>
                                                                                                                {
                                                                                                                    if (b.Value == CommandState.Blocked || b.Value == CommandState.Canceled)
                                                                                                                    {
                                                                                                                    _filterManager.RemoveFilter(_busyFilter);                                
                                                                                                                    }

                                                                                                                }));
            }

            if (OnCreateCommand != null)
            {
                OnCreateCommand(instance);
            }

            return instance;
            
        }

        public T CreateCommand<T>(params object[] args) where T : CommandBase
        {
            return (T)CreateCommand(typeof (T), args);

        }

        public Action<CommandBase> OnCreateCommand { get; set; }
    }
}