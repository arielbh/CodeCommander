using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeValue.CodeCommander;
using CodeValue.CodeCommander.Interfaces;

namespace CommandApp.Commands
{

    public abstract class BusyCommandBase : CommandBase
    {
    }

    public abstract class BuyCommandbase<T> : CommandBase<T>
    {
        
    }

    public class StopCommand : CommandBase
    {
        public override CommandState? InterpretResponse(ProcessorInput response, CommandState currentState)
        {
            return CommandState.Successed;

        }

        public override bool CanExecute()
        {
            return true;
        }

        public override void Execute()
        {
        }
    }

    public class BusyFilter : IFilter
    {
        public double Order
        {
            get; set;
        }

        public bool Process(ICommandBase command)
        {
            // When there will be stop command
            return command is StopCommand;                                

        }
    }


    public class CommandFactory
    {
        private readonly IFilterManager _filterManager;
        private BusyFilter _busyFilter;
        public CommandFactory(IFilterManager filterManager)
        {
            _filterManager = filterManager;
        }

        public T CreateCommand<T>() where T : CommandBase
        {
            T instance = Activator.CreateInstance<T>();
            if (instance is BusyCommandBase)
            {
                instance.BeforeExecuteAction = c => _filterManager.AddFilter(_busyFilter);
                instance.CompleteAction = c => _filterManager.RemoveFilter(_busyFilter);
            }
            if (instance is BusyCommandBase)
            {
                instance.CompleteAction = c => _filterManager.RemoveFilter(_busyFilter);
            }
            return instance;
        }
    }
}
