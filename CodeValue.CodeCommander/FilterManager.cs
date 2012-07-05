using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using CodeValue.CodeCommander.Interfaces;
using ReactiveUI;

namespace CodeValue.CodeCommander
{
    public class FilterManager : IFilterManager
    {
        object holdChanges = new object();
        private volatile bool _filtersChanged;

        public FilterManager()
        {
            Filters = new ReactiveCollection<IFilter>();

        }
        public bool Process(CommandBase command)
        {
            if (command.CurrentState == CommandState.Pending)
            {
                return ProcessFilters(command);
            }
            return false;
        }

        private bool ProcessFilters(CommandBase command)
        {
            IFilter[] localFilters = null;
            while (true)
            {
                lock (holdChanges)
                {
                    localFilters = Filters.OrderBy(f => f.Order).ToArray();
                    _filtersChanged = false;
                }
                if (!localFilters.Any()) return true;
                int index = 0;
                while (index < localFilters.Count() && !_filtersChanged)
                {
                    bool result = localFilters[index++].Process(command);
                    if (!result) return false;
                }
                if (!_filtersChanged) return true;
            }
        }

        public IObservable<IFilter> ItemsRemoved
        {
            get { return Filters.ItemsRemoved; }
        }

        public void AddFilter(IFilter filter)
        {
            lock (holdChanges)
            {
                _filtersChanged = true;
                Filters.Add(filter);
            }
                    
        }

        public bool RemoveFilter(IFilter filter)
        {
            lock (holdChanges)
            {
                _filtersChanged = true;
                return Filters.Remove(filter);
            }
        }

        private ReactiveCollection<IFilter> Filters { get; set; }
        public IFilter[] CurrentFilters
        {
            get
            {
                lock (holdChanges)
                {
                    
                    return Filters.ToArray();
                    
                }
            }
        }

        public IObservable<IFilter> ItemsAdded
        {
            get { return Filters.ItemsAdded; }
        }

        public IObservable<IFilter>  ItemsChanged
        {
            get { return Observable.Merge(Filters.ItemsAdded, Filters.ItemsRemoved); }
        }

    }
}