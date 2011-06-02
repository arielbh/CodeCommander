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

    public abstract class BusyCommandBase<T> : CommandBase<T>
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
