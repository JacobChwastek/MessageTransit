namespace MessageTransit.SqlTransport.Topology
{
    using MessageTransit.Topology;


    public class SqlMessageSendTopology<TMessage> :
        MessageSendTopology<TMessage>,
        ISqlMessageSendTopologyConfigurator<TMessage>
        where TMessage : class
    {
    }
}
