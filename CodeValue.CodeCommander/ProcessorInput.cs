using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeValue.CodeCommander
{
    public class ProcessorInput
    {
        public object Input { get; set; }
        
    }

    public class ProcessorInput<T> : ProcessorInput
    {
        public new T Input { get; set; }

    }
}
