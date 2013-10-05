using System;
using CodeValue.CodeCommander.Interfaces;

namespace CodeValue.CodeCommander.Tests.Mocks
{
    public class GenericFilter : IFilter
    {
        private readonly Func<ICommandBase, bool> _processAction;

        public GenericFilter(Func<ICommandBase, bool> processAction)
        {
            _processAction = processAction;
        }

        public double Order
        {
            get { return 0; }
        }

        public string Name
        {
            get { return String.Empty; }
        }

        public bool Process(ICommandBase command)
        {
            return _processAction(command);
        }
    }
}