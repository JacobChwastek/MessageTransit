#nullable enable
namespace MessageTransit.Logging
{
    public static class LogCategoryName
    {
        public const string MessageTransit = "MessageTransit";


        public static class Transport
        {
            public const string Receive = "MessageTransit.ReceiveTransport";
            public const string Send = "MessageTransit.SendTransport";
        }
    }
}
