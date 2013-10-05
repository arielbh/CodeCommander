using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CodeValue.CodeCommander.Exceptions;
using CodeValue.CodeCommander.Interfaces;
using CodeValue.CodeCommander.Logging;
using CodeValue.CodeCommander.Ordered;

namespace CodeValue.CodeCommander
{
    public class CommandProcessor : ICommandProcessor
    {
        private Subject<CommandBase> _completedCommandsSubject = new Subject<CommandBase>();
        private int _commandCounter;
        private readonly IFilterManager _filterManager;
        private readonly ILoggerFacade _loggerFacade;
        private readonly object _lockingQueue = new object();
        private readonly ObservableCollection<CommandBase> _outstandingCommands = new ObservableCollection<CommandBase>();
        private readonly Dictionary<CommandBase, IDisposable> _subscriptions = new Dictionary<CommandBase, IDisposable>();


        public CommandProcessor(IObservable<ProcessorInput> inputsSource, IFilterManager filterManager,
                                ILoggerFacade loggerFacade = null)
        {
            _outstandingCommands.CollectionChanged += _outstandingCommands_CollectionChanged;
            _loggerFacade = loggerFacade ?? new TraceLogger();
            _filterManager = filterManager;
            filterManager.ItemsChanged.Subscribe(f => TraversePendingCommands());


            if (inputsSource != null)
            {
                inputsSource.SelectMany(HandleInputResponse).Subscribe(newState =>
                {

                });
            }
        }

        protected virtual void OnItemRemoved(CommandBase item)
        {
        }

        private void _outstandingCommands_CollectionChanged(object sender,
                                                            System.Collections.Specialized.
                                                                NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (CommandBase item in e.NewItems)
                {
                    HandleAddedCommand(item);
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (CommandBase item in e.OldItems)
                {
                    _loggerFacade.Log(
                        string.Format("{2} Removing command {1} from oustanding commands. {0}",
                                      item.CommandId, item.GetType().Name,
                                      DateTime.Now.ToString("h:mm:ss.ffff")), Category.Debug, Priority.Low);
                    if (!_subscriptions.ContainsKey(item))
                    {
                        _loggerFacade.Log(
                            string.Format("{1} Command {0} doesn't contain a subscription.", item.GetType().Name,
                                          DateTime.Now.ToString("h:mm:ss.ffff")), Category.Exception, Priority.High);
                        return;
                    }
                    _subscriptions[item].Dispose();
                    _subscriptions.Remove(item);
                    OnItemRemoved(item);
                    _completedCommandsSubject.OnNext(item);
                }
            }

        }

        protected virtual IEnumerable<CommandBase> ChooseCommandsForResponse(ProcessorInput resp)
        {
            lock (_lockingQueue)
            {
                return _outstandingCommands;
            }
        }

        protected virtual IObservable<CommandState> HandleInputResponse(ProcessorInput resp)
        {
            _loggerFacade.Log(string.Format("{2} Input message {1} is recieved. {0}", resp.Input, resp.GetType().Name, DateTime.Now.ToString("h:mm:ss.ffff")), Category.Debug, Priority.Low);

            var failingCommands = new List<Tuple<CommandBase, Exception>>();
            foreach (var cmd in ChooseCommandsForResponse(resp))
            {
                bool result = false;
                try
                {
                    result = cmd.InterpretResponse(resp);
                }
                catch (Exception ex)
                {
                    failingCommands.Add(new Tuple<CommandBase, Exception>(cmd, ex));
                }

                if (result)
                {
                    cmd.CurrentState = CommandState.Successed;
                    return Observable.Return(cmd.CurrentState);
                }
            }

            foreach (var cmd in failingCommands)
            {
                cmd.Item1.CompleteCommand(cmd.Item2);
            }

            return Observable.Empty<CommandState>();
        }

        protected virtual void TraversePendingCommands()
        {
            lock (_lockingQueue)
            {
                _outstandingCommands.Where(c => c.CurrentState == CommandState.Pending)
                                    .OrderBy(c => c.Order)
                                    .ToList()
                                    .ForEach(PushToFilter);
            }
        }

        protected virtual void HandleAddedCommand(CommandBase item)
        {
            _loggerFacade.Log(string.Format("{2} Adding command {1} to oustanding commands. {0}", item.CommandId, item.GetType().Name, DateTime.Now.ToString("h:mm:ss.ffff")), Category.Debug, Priority.Low);

            item.LoggerFacade = _loggerFacade;
            item.SerialNumber = _commandCounter++;
            item.CurrentState = CommandState.Pending;
            PushToFilter(item);
            if (_subscriptions.ContainsKey(item))
            {
                _subscriptions[item].Dispose();
            }
            _subscriptions[item] = item.Subscribe(u => { },
                                                  ex =>
                                                  RemoveCommand(item),
                                                  () =>
                                                  RemoveCommand(item));
        }

        protected virtual void AddCommand(CommandBase command)
        {
            if (command.CurrentState != CommandState.New) throw new CommandProcessorException("Command is not new");
            lock (_lockingQueue)
            {
                _outstandingCommands.Add(command);
            }
        }
        protected virtual bool RemoveCommand(CommandBase item)
        {
            lock (_lockingQueue)
            {
                return _outstandingCommands.Remove(item);
            }
        }

        protected virtual void PushToFilter(CommandBase command)
        {
            Task.Factory.StartNew(() => DoPushToFilter(command));
        }

        protected void DoPushToFilter(CommandBase command)
        {
            _loggerFacade.Log(string.Format("{2} Command {1} is passed to filter manager for processsing. {0}", command.CommandId, command.GetType().Name, DateTime.Now.ToString("h:mm:ss.ffff")), Category.Debug, Priority.Low);

            Exception exception = null;
            bool result;
            try
            {
                result = _filterManager.Process(command);
            }
            catch (Exception ex)
            {
                result = false;
                exception = ex;
            }
            CommandState nextState = result ? CommandState.Executing
                                            : command.ShouldFailIfFiltered ? CommandState.Failed
                                                                           : CommandState.Pending;

            command.StartRequest(nextState, exception);
        }

        public virtual IDisposable PublishCommand(CommandBase command, IObserver<ICommandResponse<Unit>> observer = null)
        {
            _loggerFacade.Log(string.Format("{2} Command {1} is published for processing. {0}", command.CommandId, command.GetType().Name, DateTime.Now.ToString("h:mm:ss.ffff")), Category.Debug, Priority.Low);

            IDisposable subscription = null;
            if (observer != null)
                subscription = command.Subscribe(observer);
            AddCommand(command);
            return subscription;
        }

        public virtual IDisposable PublishCommand<T>(CommandBase<T> command, IObserver<ICommandResponse<T>> observer = null)
        {
            _loggerFacade.Log(string.Format("{2}Command {1} is published for processing. {0}", command.CommandId, command.GetType().Name, DateTime.Now.ToString("h:mm:ss.ffff")), Category.Debug, Priority.Low);

            IDisposable subscription = null;
            if (observer != null)
                subscription = command.Subscribe(observer);
            AddCommand(command);
            return subscription;
        }

        public virtual IDisposable[] PublishOrderedCommands(CommandBase[] commands, IObserver<ICommandResponse<Unit>>[] observers = null)
        {
            var disposables = new List<IDisposable>();
            var commandsTracking = commands.OrderBy(c => c.Order).ToDictionary(c => c as ICommandBase, c => false);
            var filter = CreateOrderedCommandsFilter(commands, commandsTracking);
            filter.UnregisterToken = RegisterForCompletedCommands(filter.CompletedCommandsObserver);
            var pusbFilterDispose =
                RegisterForCompletedCommands(Observer.Create<CommandBase>(c => TraversePendingCommands()));
            filter.Finalizer = () =>
            {
                pusbFilterDispose.Dispose();
                filter.Finalizer = null;
                _filterManager.RemoveFilter(filter);
            };

            _filterManager.AddFilter(filter);
            for (int i = 0; i < commands.Length; i++)
            {
                var command = commands[i];
                IObserver<ICommandResponse<Unit>> observer = null;

                if (observers != null)
                {
                    observer = observers.ElementAtOrDefault(i);
                }
                disposables.Add(PublishCommand(command, observer));
            }
            return disposables.ToArray();
        }

        protected virtual OrderedCommandsFilter CreateOrderedCommandsFilter(CommandBase[] commands, Dictionary<ICommandBase, bool> commandsTracking)
        {
            return new OrderedCommandsFilter(commands, commandsTracking);
        }


        public virtual void CancelCommand(CommandBase command)
        {
            _loggerFacade.Log(string.Format("{2} Command {1} is Canceled. {0}", command.CommandId, command.GetType().Name, DateTime.Now.ToString("h:mm:ss.ffff")), Category.Debug, Priority.Low);

            command.CurrentState = CommandState.Canceled;
            lock (_lockingQueue)
            {
                if (_outstandingCommands.Contains(command))
                {
                    _outstandingCommands.Remove(command);
                }
            }
        }

        public void CancelAllCommand()
        {
            CommandBase[] commandsToCancel;
            lock (_lockingQueue)
            {
                commandsToCancel = _outstandingCommands.ToArray();
            }
            foreach (var command in commandsToCancel)
            {
                CancelCommand(command);
            }
        }

        public virtual void RerunBlockedCommand(CommandBase command)
        {
            if (command.CurrentState != CommandState.Blocked) throw new CommandProcessorException("Command is not blocked");
            HandleAddedCommand(command);
        }

        public virtual void CancelCommandGroup(string groupId)
        {
            lock (_lockingQueue)
            {
                var commandsToCancel = _outstandingCommands.Where(c => c.CommandGroup == groupId).ToArray();
                foreach (var command in commandsToCancel)
                {
                    CancelCommand(command);
                }
            }
        }

        public virtual IDisposable RegisterForCompletedCommands(IObserver<CommandBase> observer)
        {
            lock (_lockingQueue)
            {
                return _completedCommandsSubject.Subscribe(observer);
            }
        }

    }




}
