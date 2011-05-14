using System.Windows.Input;

namespace CodeValue.CodeCommander.Interfaces
{
    public enum CommandState
    {
        New,
        Pending,
        Executing,
        Successed,
        Failed

    }

    public interface ICommandBase
    {
        bool CanExecute();
        void Execute();
    }
}