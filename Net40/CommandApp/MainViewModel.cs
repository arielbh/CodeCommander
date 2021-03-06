﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CodeLight.Common.Desktop;
using CodeValue.CodeCommander;
using CodeValue.CodeCommander.Interfaces;
using CommandApp.Commands;
using Microsoft.Practices.Prism.Commands;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CommandApp
{
    public class MainViewModel : ViewModelBase<object>
    {
        Subject<ProcessorInput> callMe = new Subject<ProcessorInput>();
        private NotConnectedFilter _notConnectedFilter = new NotConnectedFilter();
        private CommandFactory commandFactory;
        private Type[] _types;
        private CommandBase currentConnectCommand;
        private IProcessedCommand currentAlertCommand;
        private Dictionary<Type, Action<CommandBase>> _additionals = new Dictionary<Type, Action<CommandBase>>();
        private Dictionary<string, Func<string, bool>> _CanExecutes = new Dictionary<string, Func<string, bool>>();

        public MainViewModel()
        {
            Filters = new ObservableCollection<IFilter>();
            SendSignalCommand = new DelegateCommand<string>(s =>
                                                                {
                                                                    if (string.IsNullOrEmpty(SignalValue))
                                                                        callMe.OnNext(new DeviceResult {CommandId = s});
                                                                    else
                                                                        callMe.OnNext(new DeviceResult<string>
                                                                                          {
                                                                                              CommandId = s,
                                                                                              Input = SignalValue
                                                                                          });

                                                                }
                );

            _types = Assembly.GetExecutingAssembly().GetTypes();
            _additionals[typeof (ConnectCommand)] =
                c =>
                    {
                        CommandBase<bool> command = c as CommandBase<bool>;
                        command.CompleteAction = com =>
                                                     {
                                                         CommandBase<bool> cast = com as CommandBase<bool>;
                                                         if (cast.ReturnValue)
                                                         {
                                                             FilterManager.RemoveFilter(_notConnectedFilter);
                                                         }
                                                     };
                        currentConnectCommand = command;
                    };
            _additionals[typeof (AlertsCommand)] =
                c =>
                    {
                        CommandBase<String> com = c as CommandBase<string>;
                        com.Subscribe(
                            Observer.Create<ICommandResponse<string>>(
                                x => AddMessage(x.Sender.ToString() + " Got result " + x.Value.ToString()),
                                ex => AddMessage(ex.Source + " Got Error: " + ex.Message),
                                () => { }));
                        currentAlertCommand = c;
                    };

            _CanExecutes["AlertsCommand"] = new Func<string, bool>(s => CanConnect);

            CreateCommandCommand = new DelegateCommand<string>(CreateACommand, s =>
                                                                                   {
                                                                                       if (_CanExecutes.ContainsKey(s))
                                                                                       {
                                                                                           return _CanExecutes[s](s);
                                                                                       }
                                                                                       return true;
                                                                                   });

            ThrowAlertCommand =
                new DelegateCommand<string>(
                    s => callMe.OnNext(new DeviceResult<string> {Input = s, CommandId = currentAlertCommand.CommandId}));

            Commands = new ObservableCollection<IProcessedCommand>();
            Messages = new ObservableCollection<Message>();

            FilterManager = new FilterManager();

            FilterManager.ItemsAdded.ObserveOn(SynchronizationContext.Current).Subscribe(f => Filters.Add(f));
            FilterManager.ItemsRemoved.ObserveOn(SynchronizationContext.Current).Subscribe(f => Filters.Remove(f));
            FilterManager.AddFilter(_notConnectedFilter);
            commandFactory = new CommandFactory(FilterManager);
            commandFactory.OnCreateCommand = new Action<CommandBase>(c => Commands.Add(c));

            CommandProcessor = new CommandProcessor(callMe, FilterManager);
            CommandProcessor.RegisterForCompletedCommands(
                Observer.Create<CommandBase>(c => AddMessage(c.ToString() + "  is Completed")));

            SendConnectCommand = new DelegateCommand<bool?>(b =>
                                                            callMe.OnNext(new DeviceResult<bool>
                                                                              {
                                                                                  Input = b.Value,
                                                                                  CommandId =
                                                                                      currentConnectCommand.CommandId
                                                                              }));


            callMe.OnNext(new ProcessorInput());

            ReleaseBlockedCommand = new DelegateCommand<string>(s => CommandProcessor.RerunBlockedCommand((CommandBase)
                                                                                                          Commands.First(c => c.CommandId == s)));
            CancelCommandCommand = new DelegateCommand<string>(s => CommandProcessor.CancelCommand((CommandBase)
                                                                                                          Commands.First(c => c.CommandId == s)));

            CreateSequnceCommand = new DelegateCommand(() =>
                                                           {
                                                               SequenceCommand c1 = new SequenceCommand(this, "AAA");
                                                               SequenceCommand c2 = new SequenceCommand(this, "BBB");
                                                               SequenceCommand c3 = new SequenceCommand(this, "CCC");
                                                               SequenceCommand c4 = new SequenceCommand(this, "DDD");
                                                               Commands.Add(c1);
                                                               Commands.Add(c2);
                                                               Commands.Add(c3);
                                                               Commands.Add(c4);
                                                               subscriptions.AddRange(
                                                                   CommandProcessor.PublishOrderedCommands(
                                                                       new[] {c1, c2, c3, c4},
                                                                       new[] { MyObserver, MyObserver, MyObserver, MyObserver }));

                                                           });

        }


        public IFilterManager FilterManager { get; set; }
        private List<IDisposable> subscriptions = new List<IDisposable>();

        private void WrapAndCallCommand(CommandBase command)
        {
            subscriptions.Add(CommandProcessor.PublishCommand(command, MyObserver));
        }

        private IObserver<ICommandResponse<Unit>> MyObserver
        {
            get
            {
                return System.Reactive.Observer.Create<ICommandResponse<Unit>>(
                    x => AddMessage(x.Sender.ToString() + " Got result " + x.Value.ToString()),
                    ex => AddMessage(ex.Source + " Got Error: " + ex.Message),
                    () => { });
            }
        }


        private void CreateACommand(string commandName)
        {
            var type = _types.FirstOrDefault(t => t.Name == commandName);
            if (type == null) return;

            var command = commandFactory.CreateCommand(type, this);
            if (_additionals.ContainsKey(type))
            {
                _additionals[type](command);

            }

            WrapAndCallCommand(command);
        }

        public void AddMessage(string text)
        {
            App.Current.Dispatcher.BeginInvoke(new Action(
                                                   () => Messages.Add(new Message { Text = text })));
        }


        public DelegateCommand<string> CreateCommandCommand { get; set; }
        public DelegateCommand<string> CancelCommandCommand { get; set; }

        public DelegateCommand CreateSequnceCommand { get; set; }

        public DelegateCommand<bool?> SendConnectCommand { get; set; }
        public DelegateCommand<string> ThrowAlertCommand { get; set; }
        public DelegateCommand<string> ReleaseBlockedCommand { get; set; }

        public ICommandProcessor CommandProcessor { get; set; }
        public ObservableCollection<Message> Messages { get; set; }
        public ObservableCollection<IProcessedCommand> Commands { get; set; }
        public ObservableCollection<IFilter> Filters { get; set; }
        public DelegateCommand<string> SendSignalCommand { get; set; }
        public string SignalValue { get; set; }
        public bool AllowExecute { get; set; }

        private bool _canConnect = false;

        public bool CanConnect
        {
            get { return _canConnect; }
            set
            {
                if (value != _canConnect)
                {
                    _canConnect = value;
                    OnPropertyChanged(() => CanConnect);
                    SendConnectCommand.RaiseCanExecuteChanged();
                    CreateCommandCommand.RaiseCanExecuteChanged();
                    
                }
            }
        }


        private DelegateCommand _callBackgroundTaskCommand;

        public DelegateCommand CallBackgroundTaskCommand
        {
            get
            {
                return _callBackgroundTaskCommand ?? (_callBackgroundTaskCommand = new DelegateCommand(
                                                     () =>
                                                         {
                                                             Task.Factory.StartNew(() =>
                                                                                       {
                                                                                           var connect =
                                                                                               new ConnectCommand(this);
                                                                                           CommandProcessor p =
                                                                                               new CommandProcessor(
                                                                                                   callMe, FilterManager);
                                                                                           var dispose =
                                                                                               p.PublishCommand(connect);
                                                                                       });
                                                         }));
            }
        }






    }

    public class Message
    {
        public Message ()
        {
            Date = DateTime.Now;
        }
        public string Text { get; set; }
        public DateTime Date { get; set; }
    }
}