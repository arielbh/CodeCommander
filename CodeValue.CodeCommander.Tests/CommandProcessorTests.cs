using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Threading;
using CodeValue.CodeCommander.Exceptions;
using CodeValue.CodeCommander.Interfaces;
using CodeValue.CodeCommander.Tests.Mocks;
using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeValue.CodeCommander.Tests
{
    [TestClass]
    public class CommandProcessorTests
    {
        readonly TimeSpan _timeout = new TimeSpan(10000);
        [TestMethod] 
        public void PublishCommand_NoObserver_CommandIsChangedToPending()
        {
            var fakeFilterManager = A.Fake<IFilterManager>();
            var processor = new CommandProcessor(null, fakeFilterManager);
            
            var command = new TestCommand(CommandState.New);

            processor.PublishCommand(command);

            Assert.AreEqual(CommandState.Pending, command.CurrentState);
        }
        
        [TestMethod]
        public void PublishCommand_WithObserver_CommandIsChangedToPending()
        {
            var fakeFilterManager = A.Fake<IFilterManager>();
            var processor = new CommandProcessor(null, fakeFilterManager);
            var command = new TestCommand(CommandState.New);

            processor.PublishCommand(command, Observer.Create<ICommandResponse<Unit>>(_ => { }));

            Assert.AreEqual(CommandState.Pending, command.CurrentState);
        }

        [TestMethod]
        public void PublishCommand_CommandIsNotNew_ThrowsException()
        {
            bool wasThrown = false;
            Exception thrownException = null;

            var fakeFilterManager = A.Fake<IFilterManager>();
            var processor = new CommandProcessor(null, fakeFilterManager);

            var command = new TestCommand(CommandState.Canceled);

            try
            {
                processor.PublishCommand(command);
            }
            catch (CommandProcessorException ex)
            {
                thrownException = ex;
                wasThrown = true;
            }

            Assert.IsTrue(wasThrown);
            Assert.AreEqual("Command is not new", thrownException.Message);
        }

        [TestMethod]
        public void PublishCommand_CommandShouldFailWhenFiltered_CommandFailed()
        {
            var fakeFilterManager = A.Fake<IFilterManager>();
            var processor = new CommandProcessor(null, fakeFilterManager);

            var evt = new ManualResetEventSlim(false);
            var command = new TestCommand(CommandState.New, shouldFailIfFiltered: true);
            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Failed)
                    {
                        evt.Set();
                    }
                }
            };

            processor.PublishCommand(command);

            Assert.IsTrue(evt.Wait(_timeout));

            Assert.AreEqual(CommandState.Failed, command.CurrentState);
        }

        [TestMethod]
        public void PublishCommand_FilterThrewExceptionAndCommandIsPending_CommandStillPending()
        {
            bool isPendingWithException = false;
            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Throws(new Exception());

            var processor = new CommandProcessor(null, fakeFilterManager);
                                                
            var evt = new ManualResetEventSlim(false);
            var command = new TestCommand(CommandState.New, shouldFailIfFiltered: false,
// ReSharper disable RedundantArgumentName 
//We want to keep this verbose to clarity
                                          startRequestAction: (s, e) =>
// ReSharper restore RedundantArgumentName
                                                              {
                                                                  isPendingWithException = (s == CommandState.Pending &&
                                                                                            e != null);
                                                                  evt.Set();
                                                              });

            processor.PublishCommand(command);

            Assert.IsTrue(evt.Wait(_timeout));

            Assert.IsTrue(isPendingWithException);
        }

        [TestMethod]
        public void PublishCommand_FilterThrewExceptionAndCommandShouldFail_CommandFailed()
        {
            bool isFailedWithException = false;
            var expectedException = new Exception();
            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Throws(expectedException);

            var processor = new CommandProcessor(null, fakeFilterManager);

            var evt = new ManualResetEventSlim(false);
            var command = new TestCommand(CommandState.New, shouldFailIfFiltered: true);
            processor.PublishCommand(command, Observer.Create<ICommandResponse<Unit>>(_ => { }, ex =>
                                                                        {
                                                                            isFailedWithException = ex ==
                                                                                                    expectedException;
                                                                            evt.Set();

                                                                        }));
            Assert.IsTrue(evt.Wait(_timeout));

            Assert.AreEqual(CommandState.Failed, command.CurrentState);
            Assert.IsTrue(isFailedWithException);
        }


        [TestMethod]
        public void PublishCommand_CommandPassesFilter_CommandIsExecuted()
        {
            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);

            var processor = new CommandProcessor(null, fakeFilterManager);

            var evt = new ManualResetEventSlim(false);
            var command = new TestCommand(CommandState.New);
            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Executing)
                    {
                        evt.Set();
                    }
                }
            };


            processor.PublishCommand(command);

            Assert.IsTrue(evt.Wait(_timeout));

            Assert.AreEqual(CommandState.Executing, command.CurrentState);
        }

        [TestMethod]
        public void PublishCommandGeneric_NoObserver_CommandIsChangedToPending()
        {
            var fakeFilterManager = A.Fake<IFilterManager>();
            var processor = new CommandProcessor(null, fakeFilterManager);

            var command = new TestCommand<bool>(CommandState.New);

            processor.PublishCommand(command);

            Assert.AreEqual(CommandState.Pending, command.CurrentState);
        }

        [TestMethod]
        public void PublishCommandGeneric_WithObserver_CommandIsChangedToPending()
        {
            var fakeFilterManager = A.Fake<IFilterManager>();
            var processor = new CommandProcessor(null, fakeFilterManager);
            var command = new TestCommand<bool>(CommandState.New);

            processor.PublishCommand(command, Observer.Create<ICommandResponse<bool>>(_ => { }));

            Assert.AreEqual(CommandState.Pending, command.CurrentState);
        }


        [TestMethod]
        public void CancelCommand_CommandIsNew_CommandIsCancled()
        {
            var fakeFilterManager = A.Fake<IFilterManager>();
            var processor = new CommandProcessor(null, fakeFilterManager);
            var command = new TestCommand(CommandState.New);

            processor.CancelCommand(command);

            Assert.AreEqual(CommandState.Canceled, command.CurrentState);
        }

        [TestMethod]
        public void CancelCommand_CommandIsPending_CommandIsCancled()
        {
            var resetEvent = new ManualResetEventSlim(false);

            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(false);
            var processor = new CommandProcessor(null, fakeFilterManager);
            var command = new TestCommand(CommandState.New, shouldFailIfFiltered: false,
// ReSharper disable RedundantArgumentName
                //We want to keep this verbose to clarity
                                         startRequestAction: (s, e) => resetEvent.Set());
// ReSharper restore RedundantArgumentName
            processor.PublishCommand(command);
            Assert.IsTrue(resetEvent.Wait(_timeout));

            processor.CancelCommand(command);

            Assert.AreEqual(CommandState.Canceled, command.CurrentState);
        }

        [TestMethod]
        public void CancelCommand_CommandIsExecuting_CommandIsCancled()
        {
            var resetEvent = new ManualResetEventSlim(false);

            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);
            var processor = new CommandProcessor(null, fakeFilterManager);
            var command = new TestCommand(CommandState.New);
            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Executing)
                    {
                        resetEvent.Set();
                    }
                }
            };


            processor.PublishCommand(command);
            Assert.IsTrue(resetEvent.Wait(_timeout));

            processor.CancelCommand(command);

            Assert.AreEqual(CommandState.Canceled, command.CurrentState);
        }

        [TestMethod]
        public void CancelCommand_CommandIsBlocked_CommandIsCancled()
        {
            var resetEvent = new ManualResetEventSlim(false);

            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);
            var processor = new CommandProcessor(null, fakeFilterManager);
            var command = new TestCommand(CommandState.New, blockCanExecute:true);

            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Blocked)
                    {
                        resetEvent.Set();
                    }
                }
            };
            processor.PublishCommand(command);
            Assert.IsTrue(resetEvent.Wait(_timeout));


            processor.CancelCommand(command);

            Assert.AreEqual(CommandState.Canceled, command.CurrentState);
        }

        [TestMethod]
        public void CancelCommand_CommandIsBlockedAndShouldFailIfBlocked_CommandFails()
        {
            var resetEvent = new ManualResetEventSlim(false);

            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);
            var processor = new CommandProcessor(null, fakeFilterManager);
            var command = new TestCommand(CommandState.New, blockCanExecute: true, shouldFailIfBlocked:true);

            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Failed)
                    {
                        resetEvent.Set();
                    }
                }
            };
            processor.PublishCommand(command);
            Assert.IsTrue(resetEvent.Wait(_timeout));



            Assert.AreEqual(CommandState.Failed, command.CurrentState);
        }

        [TestMethod]
        public void CancelAllCommand_CommandsAreExecuting_CommandsAreCancled()
        {
            var resetEvent = new ManualResetEventSlim(false);
            var resetEvent2 = new ManualResetEventSlim(false);


            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);
            var processor = new CommandProcessor(null, fakeFilterManager);
            var command = new TestCommand(CommandState.New);

            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Executing)
                    {
                        resetEvent.Set();
                    }
                }
            };
            var command2 = new TestCommand(CommandState.New);

            command2.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Executing)
                    {
                        resetEvent2.Set();
                    }
                }
            };
            processor.PublishCommand(command);
            processor.PublishCommand(command2);
            Assert.IsTrue(resetEvent.Wait(_timeout));
            Assert.IsTrue(resetEvent2.Wait(_timeout));

            processor.CancelAllCommand();

            Assert.AreEqual(CommandState.Canceled, command.CurrentState);
            Assert.AreEqual(CommandState.Canceled, command2.CurrentState);
        }

        [TestMethod]
        public void RerunBlockedCommand_CommandIsNewThusNotBlocked_ThrowException()
        {
            bool wasThrown = false;
            Exception thrownException = null;

            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);

            var processor = new CommandProcessor(null, fakeFilterManager);

            var command = new TestCommand(CommandState.New);

            try
            {
                processor.RerunBlockedCommand(command);
                
            }
            catch (CommandProcessorException ex)
            {
                thrownException = ex;
                wasThrown = true;
            }

            Assert.IsTrue(wasThrown);
            Assert.AreEqual("Command is not blocked", thrownException.Message);
        }

        [TestMethod]
        public void RerunBlockedCommand_CommandIsBlocked_CommandIsPendingAgain()
        {
            var resetEvent = new ManualResetEventSlim(false);
            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);
            var processor = new CommandProcessor(null, fakeFilterManager);
            var command = new TestCommand(CommandState.New, blockCanExecute: true);

            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Blocked)
                    {
                        resetEvent.Set();
                    }
                }
            };
            processor.PublishCommand(command);
            Assert.IsTrue(resetEvent.Wait(_timeout));

            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(false);
            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Pending)
                    {
                        resetEvent.Set();
                    }
                }
            };
            processor.RerunBlockedCommand(command);
            Assert.IsTrue(resetEvent.Wait(_timeout));



            Assert.AreEqual(CommandState.Pending, command.CurrentState);     
        }

        [TestMethod]
        public void InputSource_NewMessageIsRecievedByComamnds_InterpretResponseIsCalled()
        {
            var interpretResponseCalled = false;
            var inputSource = new Subject<ProcessorInput>();
            var resetEvent = new ManualResetEventSlim(false);
            

            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);
            var processor = new CommandProcessor(inputSource, fakeFilterManager);
            var command = new TestCommand(CommandState.New, interpretResponseAction: i =>
                                                                                     {
                                                                                         interpretResponseCalled = true;
                                                                                         resetEvent.Set();
                                                                                         return false;
                                                                                     });
            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Executing)
                    {
                        resetEvent.Set();
                    }
                }
            };

            processor.PublishCommand(command);

            Assert.IsTrue(resetEvent.Wait(_timeout));


            inputSource.OnNext(new ProcessorInput());

            Assert.IsTrue(resetEvent.Wait(_timeout));


            Assert.IsTrue(interpretResponseCalled);
        }

        [TestMethod]
        public void InputSource_CommandFailsDueToInput_CommandIsFailed()
        {
            
            var commandFailed = false;
            var inputSource = new Subject<ProcessorInput>();
            var resetEvent = new ManualResetEventSlim(false);
            Exception thrownedException = null;
            var exceptionMessage = "Test";


            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);
            var processor = new CommandProcessor(inputSource, fakeFilterManager);
            var command = new TestCommand(CommandState.New, interpretResponseAction: i =>
            {
                throw new Exception(exceptionMessage);
            });

            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Executing)
                    {
                        resetEvent.Set();
                    }
                }
            };

            processor.PublishCommand(command, Observer.Create<ICommandResponse<Unit>>(_ => { }, ex =>
                                                                                                {
                                                                                                    thrownedException =
                                                                                                        ex;
                                                                                                    commandFailed = true;
                                                                                                    resetEvent.Set();

                                                                                                    
                                                                                                }));

            Assert.IsTrue(resetEvent.Wait(_timeout));



            inputSource.OnNext(new ProcessorInput());

            Assert.IsTrue(resetEvent.Wait(_timeout));


            Assert.IsTrue(commandFailed);
            Assert.AreEqual(thrownedException.Message, exceptionMessage);
        }

        [TestMethod]
        public void InputSource_CommandFailsDueToInput_CommandIsFailedAndCompleted()
        {

            var commandFailed = false;
            var inputSource = new Subject<ProcessorInput>();
            var resetEvent = new ManualResetEventSlim(false);
            var completeResetEvent = new ManualResetEventSlim(false);

            Exception thrownedException = null;
            var exceptionMessage = "Test";


            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);
            var processor = new CommandProcessor(inputSource, fakeFilterManager);
            processor.RegisterForCompletedCommands(Observer.Create<CommandBase>(c =>
            {
                completeResetEvent.Set();


            }));
            var command = new TestCommand(CommandState.New, interpretResponseAction: i =>
            {
                throw new Exception(exceptionMessage);
            });

            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Executing)
                    {
                        resetEvent.Set();
                    }
                }
            };

            processor.PublishCommand(command, Observer.Create<ICommandResponse<Unit>>(_ => { }, ex =>
            {
                thrownedException =
                    ex;
                commandFailed = true;
                resetEvent.Set();


            }));

            Assert.IsTrue(resetEvent.Wait(_timeout));



            inputSource.OnNext(new ProcessorInput());

            Assert.IsTrue(resetEvent.Wait(_timeout));

            Assert.IsTrue(completeResetEvent.Wait(_timeout));

            

            Assert.IsTrue(commandFailed);
            Assert.AreEqual(thrownedException.Message, exceptionMessage);
        }

        [TestMethod]
        public void InputSource_CommandSucceedsDueToInput_CommandIsSuccessed()
        {

            var commandSucceed = false;
            var inputSource = new Subject<ProcessorInput>();
            var resetEvent = new ManualResetEventSlim(false);


            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);
            var processor = new CommandProcessor(inputSource, fakeFilterManager);
            var command = new TestCommand(CommandState.New, interpretResponseAction: i => true);

            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Executing)
                    {
                        resetEvent.Set();
                    }
                }
            };

            processor.PublishCommand(command, Observer.Create<ICommandResponse<Unit>>(r => { 
                commandSucceed = true; 
                resetEvent.Set();
            }));

            Assert.IsTrue(resetEvent.Wait(_timeout));



            inputSource.OnNext(new ProcessorInput());

            Assert.IsTrue(resetEvent.Wait(_timeout));


            Assert.IsTrue(commandSucceed);
        }

        [TestMethod]
        public void InputSource_CommandSucceedsDueToInput_CommandIsCompleted()
        {

            var commandCompleted = false;
            var inputSource = new Subject<ProcessorInput>();
            var resetEvent = new ManualResetEventSlim(false);


            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);
            var processor = new CommandProcessor(inputSource, fakeFilterManager);
            var command = new TestCommand(CommandState.New, interpretResponseAction: i => true);

            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Executing)
                    {
                        resetEvent.Set();
                    }
                }
            };

            processor.PublishCommand(command, Observer.Create<ICommandResponse<Unit>>(_ => { }, () =>
            {
                commandCompleted = true;
                resetEvent.Set();
            }));

            Assert.IsTrue(resetEvent.Wait(_timeout));



            inputSource.OnNext(new ProcessorInput());

            Assert.IsTrue(resetEvent.Wait(_timeout));


            Assert.IsTrue(commandCompleted);
        }

        [TestMethod]
        public void InputSource_CommandSucceedsDueToInputButIsExecutingForEver_CommandSuccededButStillExecuting()
        {

            var commandSucceed = false;
            var inputSource = new Subject<ProcessorInput>();
            var resetEvent = new ManualResetEventSlim(false);


            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);
            var processor = new CommandProcessor(inputSource, fakeFilterManager);
            var command = new TestCommand(CommandState.New, interpretResponseAction: i => true, shouldExecuteForever: true);


            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Executing)
                    {
                        resetEvent.Set();
                    }
                }
            };

            processor.PublishCommand(command, Observer.Create<ICommandResponse<Unit>>(r =>
            {
                commandSucceed = true;
                resetEvent.Set();
            }));

            Assert.IsTrue(resetEvent.Wait(_timeout));



            inputSource.OnNext(new ProcessorInput());

            Assert.IsTrue(resetEvent.Wait(_timeout));


            Assert.IsTrue(commandSucceed && command.CurrentState == CommandState.Executing);
        }

        [TestMethod]
        public void CommandSelfInputSource_NewResponseIsRecievedByComamnd_InterpretResponseIsCalled()
        {
            var interpretResponseCalled = false;
            var inputSource = new Subject<ProcessorInput>();
            var resetEvent = new ManualResetEventSlim(false);

            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);

            var processor = new CommandProcessor(null, fakeFilterManager);

            var evt = new ManualResetEventSlim(false);
            var command = new TestCommand(CommandState.New, interpretResponseAction: i =>
            {
                interpretResponseCalled = true;
                resetEvent.Set();
                return false;
            });
            command.RegisterInputSource(inputSource);

            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Executing)
                    {
                        evt.Set();
                    }
                }
            };

            processor.PublishCommand(command);

//            Assert.IsTrue(evt.Wait(_timeout));
            evt.Wait();


            inputSource.OnNext(new ProcessorInput());

            Assert.IsTrue(resetEvent.Wait(_timeout));


            Assert.IsTrue(interpretResponseCalled);
        }

        [TestMethod]
        public void CommandSelfInputSource_CommandFailsDueToInput_CommandIsFailed()
        {

            var commandFailed = false;
            var inputSource = new Subject<ProcessorInput>();
            var resetEvent = new ManualResetEventSlim(false);
            Exception thrownedException = null;
            var exceptionMessage = "Test";


            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);
            var processor = new CommandProcessor(null, fakeFilterManager);
            var command = new TestCommand(CommandState.New, interpretResponseAction: i =>
            {
                throw new Exception(exceptionMessage);
            });
            command.RegisterInputSource(inputSource);

            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Executing)
                    {
                        resetEvent.Set();
                    }
                }
            };

            processor.PublishCommand(command, Observer.Create<ICommandResponse<Unit>>(_ => { }, ex =>
            {
                thrownedException =
                    ex;
                commandFailed = true;
                resetEvent.Set();


            }));

            Assert.IsTrue(resetEvent.Wait(_timeout));



            inputSource.OnNext(new ProcessorInput());

            Assert.IsTrue(resetEvent.Wait(_timeout));

            Assert.IsTrue(commandFailed);
            Assert.AreEqual(thrownedException.Message, exceptionMessage);
        }

        [TestMethod]
        public void CommandSelfInputSource_CommandSucceedsDueToInput_CommandIsSuccessed()
        {

            var commandSucceed = false;
            var inputSource = new Subject<ProcessorInput>();
            var resetEvent = new ManualResetEventSlim(false);


            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);
            var processor = new CommandProcessor(null, fakeFilterManager);
            var command = new TestCommand(CommandState.New, interpretResponseAction: i => true);
            command.RegisterInputSource(inputSource);

            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Executing)
                    {
                        resetEvent.Set();
                    }
                }
            };

            processor.PublishCommand(command, Observer.Create<ICommandResponse<Unit>>(r =>
            {
                commandSucceed = true;
                resetEvent.Set();
            }));

            Assert.IsTrue(resetEvent.Wait(_timeout));



            inputSource.OnNext(new ProcessorInput());

            Assert.IsTrue(resetEvent.Wait(_timeout));


            Assert.IsTrue(commandSucceed);
        }
        [TestMethod]
        public void CommandSelfInputSource_CommandSucceedsDueToInput_CommandIsCompleted()
        {

            var commandCompleted = false;
            var inputSource = new Subject<ProcessorInput>();
            var resetEvent = new ManualResetEventSlim(false);


            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);
            var processor = new CommandProcessor(null, fakeFilterManager);
            var command = new TestCommand(CommandState.New, interpretResponseAction: i => true);
            command.RegisterInputSource(inputSource);

            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Executing)
                    {
                        resetEvent.Set();
                    }
                }
            };

            processor.PublishCommand(command, Observer.Create<ICommandResponse<Unit>>(_ => { }, () =>
            {
                commandCompleted = true;
                resetEvent.Set();
            }));

            Assert.IsTrue(resetEvent.Wait(_timeout));



            inputSource.OnNext(new ProcessorInput());

            Assert.IsTrue(resetEvent.Wait(_timeout));


            Assert.IsTrue(commandCompleted);
        }

        [TestMethod]
        public void CommandSelfInputSource_CommandSucceedsDueToInputButIsExecutingForEver_CommandSuccededButStillExecuting()
        {

            var commandSucceed = false;
            var inputSource = new Subject<ProcessorInput>();
            var resetEvent = new ManualResetEventSlim(false);


            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);
            var processor = new CommandProcessor(null, fakeFilterManager);
            var command = new TestCommand(CommandState.New, interpretResponseAction: i => true, shouldExecuteForever: true);
            command.RegisterInputSource(inputSource);

            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Executing)
                    {
                        resetEvent.Set();
                    }
                }
            };

            processor.PublishCommand(command, Observer.Create<ICommandResponse<Unit>>(r =>
            {
                commandSucceed = true;
                resetEvent.Set();
            }));

            Assert.IsTrue(resetEvent.Wait(_timeout));



            inputSource.OnNext(new ProcessorInput());

            Assert.IsTrue(resetEvent.Wait(_timeout));


            Assert.IsTrue(commandSucceed && command.CurrentState == CommandState.Executing);
        }

        [TestMethod]
        public void CommandTimeouts_CommandHasElapsingPendingTimeout_CommandFails()
        {
            bool wasThrown = false;
            Exception thrownException = null;
            var resetEvent = new ManualResetEventSlim(false);


            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Invokes(a => Thread.Sleep(1000));

            var processor = new CommandProcessor(null, fakeFilterManager);

            var command = new TestCommand(CommandState.New, pendingTimeout:new TimeSpan(500));

            processor.PublishCommand(command,Observer.Create<ICommandResponse<Unit>>(_ => { }, ex =>
                                                                                               {
                                                                                                   thrownException = ex;
                                                                                                   wasThrown = true;
                                                                                                   resetEvent.Set();
                                                                                               }));
            Assert.IsTrue(resetEvent.Wait(_timeout));


            Assert.IsTrue(wasThrown);
            Assert.AreEqual("Command has exceded its Pending timeout", thrownException.Message);
        }

        [TestMethod]
        public void CommandTimeouts_CommandHasElapsingExecutionTimeout_CommandFails()
        {
            bool wasThrown = false;
            Exception thrownException = null;
            var resetEvent = new ManualResetEventSlim(false);


            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);

            var processor = new CommandProcessor(null, fakeFilterManager);

            var command = new TestCommand(CommandState.New,  executionTimeout: new TimeSpan(500), executeAction: () => Thread.Sleep(1000));

            processor.PublishCommand(command, Observer.Create<ICommandResponse<Unit>>(_ => { }, ex =>
            {
                thrownException = ex;
                wasThrown = true;
                resetEvent.Set();
            }));
            //Assert.IsTrue(resetEvent.Wait(_timeout));
            resetEvent.Wait();


            Assert.IsTrue(wasThrown);
            Assert.AreEqual("Command has exceded its Executing timeout", thrownException.Message);
        }

        [TestMethod]
        public void CommandPublish_CommandThrowsExceptionWhileExecuting_CommandFailsWithThatException()
        {
            bool wasThrown = false;
            Exception thrownException = new Exception("Text");
            Exception expectedException = null;
            var resetEvent = new ManualResetEventSlim(false);


            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);

            var processor = new CommandProcessor(null, fakeFilterManager);

            var command = new TestCommand(CommandState.New, executeAction: () =>
                                                                           {
                                                                               throw thrownException;
                                                                           });
            

            processor.PublishCommand(command, Observer.Create<ICommandResponse<Unit>>(_ => { }, ex =>
            {
                expectedException = ex;
                wasThrown = true;
                resetEvent.Set();
            }));
            Assert.IsTrue(resetEvent.Wait(_timeout));


            Assert.IsTrue(wasThrown);
            Assert.AreEqual(expectedException, thrownException);
        }

        [TestMethod]
        public void CommandPublish_CommandShouldCompleteWhenAfterExecuting_CommandSucceded()
        {
            var resetEvent = new ManualResetEventSlim(false);

            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);

            var processor = new CommandProcessor(null, fakeFilterManager);
            var command = new TestCommand(CommandState.New, shouldCompleteAfterExecute: true);

            processor.PublishCommand(command, Observer.Create<ICommandResponse<Unit>>(_ => { }, resetEvent.Set));
            //Assert.IsTrue(resetEvent.Wait(new TimeSpan(0,0,20)));
            resetEvent.Wait();

            Assert.AreEqual(CommandState.Successed, command.CurrentState);


        }

        [TestMethod]
        public void CommandPublish_CommandHasBeforeExecutingAction_ActionCalled()
        {
            var actionCalled = false;
            var resetEvent = new ManualResetEventSlim(false);

            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);

            var processor = new CommandProcessor(null, fakeFilterManager);
            var command = new TestCommand(CommandState.New, beforeExecuteAction: i =>
                                                                                 {
                                                                                     actionCalled = true;
                                                                                     resetEvent.Set();
                                                                                 });
            processor.PublishCommand(command);

            Assert.IsTrue(resetEvent.Wait(_timeout));


            Assert.IsTrue(actionCalled);


        }

        [TestMethod]
        public void CommandPublish_CommandHasFullfillmentAction_ActionCalled()
        {
            var actionCalled = false;
            var resetEvent = new ManualResetEventSlim(false);
            var inputSource = new Subject<ProcessorInput>();


            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);

            var processor = new CommandProcessor(inputSource, fakeFilterManager);
            var command = new TestCommand(CommandState.New, interpretResponseAction: r => true, fullfillmentAction: i =>
                                                                                                                    {
                                                                                                                        actionCalled
                                                                                                                            =
                                                                                                                            true;
                                                                                                                        resetEvent
                                                                                                                            .
                                                                                                                            Set
                                                                                                                            ();
                                                                                                                    });
            processor.PublishCommand(command);
            inputSource.OnNext(new ProcessorInput());

            Assert.IsTrue(resetEvent.Wait(_timeout));


            Assert.IsTrue(actionCalled);
        }

        [TestMethod]
        public void CommandPublish_CommandHasErrorAction_ActionCalled()
        {
            var actionCalled = false;
            var resetEvent = new ManualResetEventSlim(false);

            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);

            var processor = new CommandProcessor(null, fakeFilterManager);
            var command = new TestCommand(CommandState.New
                                          , errorAction: (i, ex) =>
                                                         {
                                                             actionCalled = true;
                                                             resetEvent.Set();
                                                         },
                                          executeAction: new Action(() =>
                                                                    {
                                                                        throw new Exception();
                                                                    }));
            processor.PublishCommand(command);

            Assert.IsTrue(resetEvent.Wait(_timeout));


            Assert.IsTrue(actionCalled);
        }

        [TestMethod]
        public void CommandPublish_CommandHasCompletedAction_ActionCalled()
        {
            var actionCalled = false;
            var resetEvent = new ManualResetEventSlim(false);
            var inputSource = new Subject<ProcessorInput>();


            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);

            var processor = new CommandProcessor(inputSource, fakeFilterManager);
            var command = new TestCommand(CommandState.New, interpretResponseAction: r => true, completeAction: i =>
            {
                actionCalled = true;
                resetEvent.Set();
            });
            processor.PublishCommand(command);
            inputSource.OnNext(new ProcessorInput());

            Assert.IsTrue(resetEvent.Wait(_timeout));


            Assert.IsTrue(actionCalled);
        }

        [TestMethod]
        public void CommandPublishGeneric_CommandGotFullfillmentWithReturnValue_ReturnValue()
        {
            var resetEvent = new ManualResetEventSlim(false);
            var inputSource = new Subject<ProcessorInput>();


            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(true);

            var processor = new CommandProcessor(inputSource, fakeFilterManager);
            var command = new TestCommand<bool>(CommandState.New, returnValue:true, interpretResponseAction: r => true, fullfillmentAction: i => resetEvent.Set());
            processor.PublishCommand(command);
            inputSource.OnNext(new ProcessorInput());

            Assert.IsTrue(resetEvent.Wait(_timeout));


            Assert.IsTrue(command.ReturnValue);
        }

        [TestMethod]
        public void CancelCommandGroup_CancelAGroup_AllCommandsInGroupAreCancelled()
        {
            int[] commandsCanceled = {0};
            var fakeFilterManager = A.Fake<IFilterManager>();
            A.CallTo(() => fakeFilterManager.Process(A<CommandBase>.Ignored)).Returns(false);

            var processor = new CommandProcessor(null, fakeFilterManager);
            var commands = new[]
                           {
                               new TestCommand(CommandState.New, groupId: "GroupA"),
                               new TestCommand(CommandState.New, groupId: "GroupB"),
                               new TestCommand(CommandState.New, groupId: "GroupA"),
                               new TestCommand(CommandState.New, groupId: "GroupA"),
                               new TestCommand(CommandState.New, groupId: "GroupB"),
                               new TestCommand(CommandState.New, groupId: "GroupA"),
                               new TestCommand(CommandState.New, groupId: "GroupC"),

                           };
            foreach (var command in commands)
            {
                TestCommand command1 = command;
                command.PropertyChanged += (o, e) =>
                {
                    if (e.PropertyName == "CurrentState")
                    {
                        if (command1.CurrentState == CommandState.Canceled)
                        {
                            commandsCanceled[0]++;

                        }
                    }
                };

                processor.PublishCommand(command);
            }

            processor.CancelCommandGroup("GroupA");

            Assert.AreEqual(4, commandsCanceled[0]);
        }

        [TestMethod]
        public void PublishOrderedCommands_OneCommandInCommands_ThisCommandIsExecuted()
        {
            var resetEvent = new ManualResetEventSlim(false);

            var filterManager = new FilterManager();
            var processor = new CommandProcessor(null, filterManager);
            var command = new TestCommand(CommandState.New);
            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Executing)
                    {
                        resetEvent.Set();
                    }
                }
            };
            processor.PublishOrderedCommands(new[] {command});

            Assert.IsTrue(resetEvent.Wait(_timeout));


            Assert.AreEqual(CommandState.Executing, command.CurrentState);

        }

     //   [TestMethod]
        ///TODO: Fix this test!
        public void PublishOrderedCommands_TwoCommandsAreSendInOrder_CommandsAreExecutedInOrder()
        {
            var resetEvent = new ManualResetEventSlim(false);
            var commandsCompleted = new bool[2];

            var filterManager = new FilterManager();
            var processor = new CommandProcessor(null, filterManager);
            var command = new TestCommand(CommandState.New, shouldCompleteAfterExecute:false) { Order = 13};
            command.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Successed)
                    {
                        commandsCompleted[0] = true;
                    }
                }
            };


            var command2 = new TestCommand(CommandState.New, shouldCompleteAfterExecute: true) { Order = 2 };
            command2.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "CurrentState")
                {
                    if (command.CurrentState == CommandState.Successed)
                    {
                        commandsCompleted[1] = true;
                        resetEvent.Set();
                    }
                }
            };

            processor.PublishOrderedCommands(new[] { command, command2 });

            Assert.IsTrue(resetEvent.Wait(_timeout));


            Assert.IsTrue(commandsCompleted.All(f => f));
        }



        //[TestMethod]
        //public void CommandProcessor_ExplicitBackgroundDispatcherNeeded_DispatcherIsEventLoops()
        //{
        //    var fakeFilterManager = A.Fake<IFilterManager>();
        //    var processor = new CommandProcessor(null, fakeFilterManager, true);
        //    Assert.IsTrue(RxApp.DeferredScheduler is EventLoopScheduler);
        //}
    }
}