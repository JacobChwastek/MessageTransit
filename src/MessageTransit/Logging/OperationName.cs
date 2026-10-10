#nullable enable
namespace MessageTransit.Logging
{
    public static class OperationName
    {
        public static class Consumer
        {
            public const string Consume = "MessageTransit.Consumer.Consume";
            public const string Handle = "MessageTransit.Consumer.Handle";
        }


        public static class Saga
        {
            public const string Send = "MessageTransit.Saga.Send";
            public const string SendQuery = "MessageTransit.Saga.SendQuery";
            public const string Initiate = "MessageTransit.Saga.Initiate";
            public const string Orchestrate = "MessageTransit.Saga.Orchestrate";
            public const string InitiateOrOrchestrate = "MessageTransit.Saga.InitiateOrOrchestrate";
            public const string Observe = "MessageTransit.Saga.Observe";
            public const string RaiseEvent = "MessageTransit.Saga.RaiseEvent";
        }


        public static class Courier
        {
            public const string Execute = "MessageTransit.Activity.Execute";
            public const string Compensate = "MessageTransit.Activity.Compensate";
        }
    }
}
