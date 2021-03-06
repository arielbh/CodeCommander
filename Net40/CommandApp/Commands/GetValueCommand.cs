using CodeValue.CodeCommander;
using CodeValue.CodeCommander.Interfaces;
using CommandApp.Commands;

namespace CommandApp
{
    public class GetValueCommand : BusyCommandBase<string>
    {
        private readonly MainViewModel _mainViewModel;

        public GetValueCommand(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            ShouldFailIfFiltered = true;
        }

        public override bool InterpretResponse(ProcessorInput response)
        {
            DeviceResult<string> res = response as DeviceResult<string>;
            if (res != null)
            {
                if (CommandId == res.CommandId)
                {
                    ReturnValue = res.Input;
                    return true;
                }
            }
            return false;
        }

        public override bool CanExecute()
        {
            _mainViewModel.AddMessage("Execute Message CanExecuted");

            return true;
        }

        public override void Execute()
        {
            _mainViewModel.AddMessage("Execute Message Executed");
            
        }
    }
}