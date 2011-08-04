# CodeCommander

CodeCommander is a commands handling framework built on top of the Reactive Framework and 
ReactiveUI framework. 

## Purpose

1.  Unified implementation of command handling behaviors between different domains. 
2.  Simple API for handling command execution, runtime behaviors and inputs. 
3.  Efficient command execution and filtering. 
4.  Supporting typical scenarios for commands: one-way, async, lifetime, limited, etc. 

## Documentation

A PDF document is provided with the code base.


## Full Sample included

When you download the code you have a full featured sample app you can execute and explore the various features.

## Using the framework

1. In order to use the framework, a client need to provide implementations for Commands 
and Filters.  It is possible to derive from CommandBase or CommandBase<T>.  Filters 
usually derive from FilterBase. 

2. The client need to provide an IFilterManager, it is recommended to use the built in 
FilterManager the framework provides but any implementation is suitable. 
The client may add and remove filters as it see fits throught out the life time of the 
command processing. 

3. Setting up an Observer to provide inputs for the CommandProcessor.  

4.The client need to create an ICommandProcessor. It is recommended to use the build in 
CommandProcessor the framework provides. The default implementation require inputs 
observer and a FilterManager. 

5. You are all set to go. Create an instance of a command and publish it to the 
CommandProcessor. 

6. Register for notifications with the Observerable returned by the publish, and keep track of 
your command. 

## Contact

Contact me at twitter : @arielbh.
I would love to hear your feedback on that.
 
