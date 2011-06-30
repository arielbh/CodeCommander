using System;
using System.Threading.Tasks;
using CodeValue.CodeCommander;
using CodeValue.CodeCommander.Interfaces;

namespace CommandApp
{
    public class ConnectCommand : CommandBase<bool>
    {
        private readonly MainViewModel _mainViewModel;

        public ConnectCommand(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            ShouldFailIfFiltered = true;
            ExecutingTimeout = 5000;
        }

        public override bool CanExecute()
        {
            _mainViewModel.AddMessage("Connect Message CanExecuted");
            return true;
        }

        protected override void HandleError(Exception ex)
        {
            _mainViewModel.CanConnect = false;
        }

        

        public override void Execute()
        {
            _mainViewModel.AddMessage("Connect Message Executed");
        }

        public override CommandState? InterpretResponse(ProcessorInput response, CommandState currentState)
        {

            DeviceResult<bool> res = response as DeviceResult<bool>;
            if (res != null)
            {
                if (CommandId == res.CommandId)
                {
                    ReturnValue = res.Input;
                    _mainViewModel.CanConnect = ReturnValue;

                    return CommandState.Successed;
                }
            }
            return null;
        }

         

    }
}