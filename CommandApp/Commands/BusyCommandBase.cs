using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeValue.CodeCommander;
using CodeValue.CodeCommander.Interfaces;

namespace CommandApp.Commands
{
    public interface IBusyCommandBase
    {
    }


    public abstract class BusyCommandBase : CommandBase, IBusyCommandBase
    {
    }

    public abstract class BusyCommandBase<T> : CommandBase<T>, IBusyCommandBase
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

    public class BusyFilter : FilterBase
    {
        public override bool Process(ICommandBase command)
        {
            // When there will be stop command
            return command is StopCommand;                                
        }
    }
}
