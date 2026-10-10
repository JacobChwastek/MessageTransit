namespace MessageTransit.SqlTransport.Configuration
{
    using Topology;


    public interface ISqlConsumeTopologySpecification :
        ISpecification
    {
        void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
    }
}
