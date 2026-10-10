#nullable enable
namespace MessageTransit.Transports.Fabric
{
    public interface IMessageExchange<T> :
        IMessageSink<T>,
        IMessageSource<T>
        where T : class
    {
        string Name { get; }
    }
}
