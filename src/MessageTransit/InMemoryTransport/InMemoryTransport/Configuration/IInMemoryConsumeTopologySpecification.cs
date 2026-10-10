namespace MessageTransit.InMemoryTransport.Configuration
{
    using MessageTransit.Configuration;


    public interface IInMemoryConsumeTopologySpecification :
        ISpecification
    {
        void Apply(IMessageFabricConsumeTopologyBuilder builder);
    }
}
