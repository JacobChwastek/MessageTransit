#nullable enable
namespace MessageTransit.AmazonSqsTransport.Tests.Persistence
{
    public class SimpleMessage
    {
        public MessageData<string>? BigData { get; set; }
    }
}
