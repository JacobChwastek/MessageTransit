namespace MessageTransit.Configuration
{
    public interface IConsumerMetadataCache<T>
    {
        IMessageInterfaceType[] ConsumerTypes { get; }
    }
}
