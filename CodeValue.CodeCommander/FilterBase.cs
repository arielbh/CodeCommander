using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeValue.CodeCommander.Interfaces;

namespace CodeValue.CodeCommander
{
    public abstract class FilterBase : IFilter
    {
        public double Order { get; protected set; }

        public string Name
        {
            get { return ToString(); }
        }

        public abstract bool Process(ICommandBase command);
    
    }
}
