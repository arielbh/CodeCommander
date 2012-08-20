using CodeValue.CodeCommander;

namespace CommandApp
{
    class DeviceResult : ProcessorInput
    {                 
        public string CommandId { get; set; }
    }

    class DeviceResult<T> : ProcessorInput<T>
    {
        public string CommandId { get; set; }

    }
}