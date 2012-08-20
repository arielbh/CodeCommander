using CodeValue.CodeCommander;

namespace CommandApp.Commands
{

    public class SequenceCommand: BusyCommandBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly string _title;

        public SequenceCommand(MainViewModel mainViewModel, string title)
        {
            _mainViewModel = mainViewModel;
            _title = title;

        }

        public override bool CanExecute()
        {
            _mainViewModel.AddMessage("Sequence [" + _title + "] CanExecuted");
            return true;
        }

        public override void Execute()
        {
            _mainViewModel.AddMessage("Sequence [" + _title + "] Executed");

        }

        public override bool InterpretResponse(ProcessorInput response)
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