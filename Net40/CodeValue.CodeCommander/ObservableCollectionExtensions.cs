using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeValue.CodeCommander
{
    public static class ObservableCollectionExtensions
    {
        public static IObservable<EventPattern<NotifyCollectionChangedEventArgs>> GetObservableChanges<T>(this ObservableCollection<T> collection)
        {
            return Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>
                (
                    handler => collection.CollectionChanged += handler,
                    handler => collection.CollectionChanged -= handler
                );
        }

        private static void Collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public static IObservable<T> GetObservableAddedValues<T>(
            this ObservableCollection<T> collection)
        {
            return collection.GetObservableChanges()
                .Where(args => args.EventArgs.Action == NotifyCollectionChangedAction.Add)
                .SelectMany(args => args.EventArgs.NewItems.Cast<T>());
        }

        public static IObservable<T> GetObservableRemovedValues<T>(
    this ObservableCollection<T> collection)
        {
            return collection.GetObservableChanges()
                .Where(args => args.EventArgs.Action == NotifyCollectionChangedAction.Remove || 
                (args.EventArgs.Action == NotifyCollectionChangedAction.Reset && args.EventArgs.OldItems.Count > 0))
                .SelectMany(args => args.EventArgs.OldItems.Cast<T>());
        }
    }
}
