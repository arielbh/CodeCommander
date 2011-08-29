using System;
using CodeValue.CodeCommander;
using CodeValue.CodeCommander.Interfaces;
using CommandApp.Commands;

namespace CommandApp
{
    public class ExecuteCommand : BusyCommandBase
    {
        private readonly MainViewModel _mainViewModel;

        public ExecuteCommand(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            PendingTimeout = new TimeSpan(10000);
            //ShouldCompleteAfterExecute = true;

        }

        public override bool CanExecute()
        {
            _mainViewModel.AddMessage("Execute Message CanExecuted");
            return _mainViewModel.AllowExecute;
        }

        public override void Execute()
        {
            _mainViewModel.AddMessage("Execute Message Executed");

        }

        public override bool InterpretResponse(ProcessorInput response, CommandState currentState)
        {
            DeviceResult res = response as DeviceResult;
            if (res != null)
            {
                if (CommandId == res.CommandId)
                {
                    return true;
                }
            }
            return false;
        }

    }
}