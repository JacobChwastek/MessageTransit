namespace MessageTransit.AmazonSqsTransport.Topology;

using MessageTransit.Topology;


public class AmazonSqsMessageSendTopology<TMessage> :
    MessageSendTopology<TMessage>,
    IAmazonSqsMessageSendTopologyConfigurator<TMessage>
    where TMessage : class
{
}
