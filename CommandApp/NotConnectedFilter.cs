using CodeValue.CodeCommander;
using CodeValue.CodeCommander.Interfaces;

namespace CommandApp
{
    public class NotConnectedFilter : FilterBase
    {
    
        public override bool Process(ICommandBase command)
        {
            return command is ConnectCommand;
        }
    }
}