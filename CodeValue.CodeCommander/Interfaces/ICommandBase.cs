using System.Windows.Input;

namespace CodeValue.CodeCommander.Interfaces
{
    public enum CommandState
    {
        New,
        Pending,
        Blocked,
        Executing,
        Successed,
        Failed,
        Canceled
    }

    public interface ICommandBase
    {
        bool CanExecute();
        void Execute();
    }
}