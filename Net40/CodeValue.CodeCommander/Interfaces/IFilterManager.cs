using System;

namespace CodeValue.CodeCommander.Interfaces
{
    public interface IFilterManager
    {
        bool Process(CommandBase command);

        void AddFilter(IFilter filter);
        bool RemoveFilter(IFilter filter);

        IObservable<IFilter> ItemsAdded { get; }
        IObservable<IFilter> ItemsRemoved { get; }
        IObservable<IFilter> ItemsChanged { get; }
    }
}