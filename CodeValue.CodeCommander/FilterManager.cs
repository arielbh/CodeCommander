using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CodeValue.CodeCommander.Interfaces;
using CodeValue.CodeCommander.Logging;

namespace CodeValue.CodeCommander
{
    public class FilterManager : IFilterManager
    {
        
        private readonly ILoggerFacade _loggerFacade;
        readonly object _holdChanges = new object();
        private volatile bool _filtersChanged;

        public FilterManager(ILoggerFacade loggerFacade = null)
        {
            ItemsAdded = new Subject<IFilter>();
            ItemsRemoved = new Subject<IFilter>();
            _loggerFacade = loggerFacade ?? new TraceLogger();
            Filters = new ObservableCollection<IFilter>();
            Filters.CollectionChanged +=Filters_CollectionChanged;
        }

        private void Filters_CollectionChanged(object sender,
                                               System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (IFilter filter in e.NewItems)
                    (ItemsAdded as Subject<IFilter>).OnNext(filter);
            }
            if (e.OldItems != null)
            {
                foreach (IFilter filter in e.OldItems)
                    (ItemsRemoved as Subject<IFilter>).OnNext(filter);
            }
        }

        public bool Process(CommandBase command)
        {
            _loggerFacade.Log("Command is being processed by FilterManager " + command, Category.Debug, Priority.Low);
            if (command.CurrentState == CommandState.Pending)
            {
                return ProcessFilters(command);
            }
            return false;
        }

        private bool ProcessFilters(CommandBase command)
        {
            while (true)
            {
                IFilter[] localFilters;
                lock (_holdChanges)
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

        public void AddFilter(IFilter filter)
        {
            lock (_holdChanges)
            {
                _filtersChanged = true;
                Filters.Add(filter);
            }
            _loggerFacade.Log("Filter + " + filter.Name + " is added to FilterManager", Category.Debug, Priority.Low);
                    
        }

        public bool RemoveFilter(IFilter filter)
        {
            _loggerFacade.Log("Filter + " + filter.Name + " is removed to FilterManager", Category.Debug, Priority.Low);
            lock (_holdChanges)
            {
                _filtersChanged = true;
                return Filters.Remove(filter);
            }
        }

        private ObservableCollection<IFilter> Filters { get; set; }
        public IFilter[] CurrentFilters
        {
            get
            {
                lock (_holdChanges)
                {
                    
                    return Filters.ToArray();
                }
            }
        }

        public IObservable<IFilter> ItemsAdded { get; private set; }
        public IObservable<IFilter> ItemsRemoved { get; private set; }
        public IObservable<IFilter> ItemsChanged { get { return ItemsAdded.Merge(ItemsRemoved); } }
    }
}