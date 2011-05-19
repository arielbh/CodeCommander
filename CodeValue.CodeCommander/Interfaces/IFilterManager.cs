using System;
using ReactiveUI;

namespace CodeValue.CodeCommander.Interfaces
{
    public interface IFilterManager
    {
        bool Process(CommandBase command);
        IObservable<IFilter> ItemsAdded { get; }
        IObservable<IFilter> ItemsRemoved { get; }
        void AddFilter(IFilter filter);
        bool RemoveFilter(IFilter filter);
    }
}