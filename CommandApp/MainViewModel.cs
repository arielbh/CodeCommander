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
        AsyncSubject<CommandResponse> callMe = new AsyncSubject<CommandResponse>();
        public MainViewModel()
        {
            Messages = new ObservableCollection<Message>();
            CreateNewCommand = new DelegateCommand(NewCommandAction);
            CommandProcessor = new CommandProcessor(callMe);

        }

        private void NewCommandAction()
        {         
            
            var connectCommand = new ConnectCommand(this);
            
            var suscriber = CommandProcessor.PublishCommand(connectCommand);
            IDisposable subscription = suscriber.Subscribe(
                x => Console.WriteLine("OnNext:  {0}", x),
                ex => Console.WriteLine("OnError: {0}", ex.Message),
                () => Console.WriteLine("OnCompleted")
            );
            if (num++ == 3)
            {
            //    CommandProcessor.Ready = true;

            }        
        }

        public DelegateCommand CreateNewCommand { get; set; }
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

        public override CommandState? InterpretResponse(CommandResponse response, CommandState currentState)
        {
            return CommandState.Successed;
        }

        
        public override void StartRequest(CommandState currentState)
        {
            SignalCommandIsInitiated();
        }
    }
}