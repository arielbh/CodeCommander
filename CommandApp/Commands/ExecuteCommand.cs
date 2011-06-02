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
            PendingTimeout = 10000;
           
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

        public override CommandState? InterpretResponse(ProcessorInput response, CommandState currentState)
        {
            DeviceResult res = response as DeviceResult;
            if (res != null)
            {
                if (CommandId == res.CommandId)
                {
                    return CommandState.Successed;
                }
            }
            return null;
        }

    }
}