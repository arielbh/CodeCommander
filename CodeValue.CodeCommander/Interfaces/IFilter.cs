namespace CodeValue.CodeCommander.Interfaces
{
    public interface IFilter
    {
        double Order { get; }
        string Name { get; }

        bool Process(ICommandBase command);
    }
}