using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Subjects;
using CodeLight.Common.Desktop;
using CodeValue.CodeCommander;
using CodeValue.CodeCommander.Interfaces;
using Microsoft.Practices.Prism.Commands;

namespace CommandApp
{
    public class MainViewModel : ViewModelBase<object>
    {
        private int num;
        AsyncSubject<ProcessorInput> callMe = new AsyncSubject<ProcessorInput>();
        private NotConnectedFilter _notConnectedFilter = new NotConnectedFilter();
        public MainViewModel()
        {
            Messages = new ObservableCollection<Message>();
            CreateNewCommand = new DelegateCommand(NewCommandAction);
            FilterManager = new FilterManager();
            FilterManager.AddFilter(_notConnectedFilter);
            CommandProcessor = new CommandProcessor(callMe, FilterManager);
            RemoveCommand = new DelegateCommand(() => FilterManager.RemoveFilter(_notConnectedFilter));

        }

        public IFilterManager FilterManager { get; set; }
        private IDisposable subscription;
        private IDisposable subscription2;

        private void WrapAndCallCommand(CommandBase command)
        {
            //CommandProcessor.PublishCommand(command).Subscribe(x => AddMessage());
            
        }

        private void NewCommandAction()
        {         
            
            var connectCommand = new ConnectCommand(this);
            
            var suscriber = CommandProcessor.PublishCommand(connectCommand);
            subscription = suscriber.Subscribe(
                x => { Console.WriteLine("OnNext: {0}", x); },
                ex => Console.WriteLine("OnError: {0}", ex.Message),
                () => Console.WriteLine("OnCompleted")
            );

            var suscriber2 = CommandProcessor.PublishCommand(new ExecuteCommand(this));

            subscription2 = suscriber2.Subscribe(
                x => { Console.WriteLine("OnNext: {0}", x); },
                ex => Console.WriteLine("OnError: {0}", ex.Message),
                () => Console.WriteLine("OnCompleted")
            );
            var c = new GetValueCommand(this);

            //CommandProcessor.PublishCommand<double>(c).Subscribe();


            //callMe.OnNext(null);
            //if (num++ == 3)
            //{
            ////    CommandProcessor.Ready = true;

            //}        
        }

        public DelegateCommand CreateNewCommand { get; set; }
        public DelegateCommand RemoveCommand { get; set; }

        public ICommandProcessor CommandProcessor { get; set; }
        public ObservableCollection<Message> Messages { get; set; }

        public void AddMessage(string text)
        {
            App.Current.Dispatcher.BeginInvoke(new Action(
                () => Messages.Add(new Message {Text = text})));
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

    public class ConnectCommand : CommandBase
    {
        private readonly MainViewModel _mainViewModel;

        public ConnectCommand(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        public override bool CanExecute()
        {
            _mainViewModel.AddMessage("Connect Message CanExecuted");
            return true;
        }

        public override void Execute()
        {
            _mainViewModel.AddMessage("Connect Message Executed");
            
        }

        public override CommandState? InterpretResponse(ProcessorInput response, CommandState currentState)
        {
            return CommandState.Successed;
        }

    }

    public class ExecuteCommand : CommandBase
    {
        private readonly MainViewModel _mainViewModel;

        public ExecuteCommand(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            PendingTimeout = 2000;
           
        }

        public override bool CanExecute()
        {
            _mainViewModel.AddMessage("Execute Message CanExecuted");
            return true;
        }

        public override void Execute()
        {
            _mainViewModel.AddMessage("Execute Message Executed");

        }

        public override CommandState? InterpretResponse(ProcessorInput response, CommandState currentState)
        {
            return CommandState.Successed;
        }

    }

    public class GetValueCommand : CommandBase<double>
    {
        private readonly MainViewModel _mainViewModel;

        public GetValueCommand(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        public override CommandState? InterpretResponse(ProcessorInput response, CommandState currentState)
        {
            return CommandState.Successed;
        }

        public override bool CanExecute()
        {
            _mainViewModel.AddMessage("Execute Message CanExecuted");

            return true;
        }

        public override void Execute()
        {
            _mainViewModel.AddMessage("Execute Message Executed");
            
        }
    }

    public class NotConnectedFilter : IFilter
    {
        public double Order { get; set; }

        public bool Process(ICommandBase command)
        {
            return command is ConnectCommand;
        }
    }
}