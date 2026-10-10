namespace MessageTransit.Configuration
{
    public interface IRedeliveryPipeSpecification
    {
        RedeliveryOptions Options { get; set; }
    }
}
