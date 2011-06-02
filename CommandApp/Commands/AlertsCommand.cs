using CodeValue.CodeCommander;
using CodeValue.CodeCommander.Interfaces;

namespace CommandApp
{
    public class AlertsCommand : CommandBase<string>
    {
        private readonly MainViewModel _mainViewModel;

        public AlertsCommand(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            ShouldExecuteForever = true;
        }

        public override CommandState? InterpretResponse(ProcessorInput response, CommandState currentState)
        {
            DeviceResult<string> res = response as DeviceResult<string>;
            if (res != null)
            {
                if (CommandId == res.CommandId)
                {
                    ReturnValue = res.Input;
                    return CommandState.Successed;
                }
            }
            return null;
        }

        public override bool CanExecute()
        {
            return true;
        }

        public override void Execute()
        {
        }
    }
}