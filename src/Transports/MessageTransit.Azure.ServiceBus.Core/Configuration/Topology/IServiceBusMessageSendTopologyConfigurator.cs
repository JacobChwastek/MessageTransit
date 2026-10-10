namespace MessageTransit
{
    public interface IServiceBusMessageSendTopologyConfigurator<TMessage> :
        IMessageSendTopologyConfigurator<TMessage>,
        IServiceBusMessageSendTopology<TMessage>,
        IServiceBusMessageSendTopologyConfigurator
        where TMessage : class
    {
    }


    public interface IServiceBusMessageSendTopologyConfigurator :
        IMessageSendTopologyConfigurator
    {
    }
}
