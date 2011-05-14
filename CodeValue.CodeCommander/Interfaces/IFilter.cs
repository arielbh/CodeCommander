namespace CodeValue.CodeCommander.Interfaces
{
    public interface IFilter
    {
        double Order { get; set; }

        bool Process(ICommandBase command);
    }
}