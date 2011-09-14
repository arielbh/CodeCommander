using System;
using CodeValue.CodeCommander;
using CodeValue.CodeCommander.Interfaces;

namespace CommandApp
{
    public class NotConnectedFilter : FilterBase
    {
    
        public override bool Process(ICommandBase command)
        {
            
            var res = command is ConnectCommand;
            if (!res)
            {
                throw new Exception("My Exception");
            }
            return res;
        }
    }
}