namespace MessageTransit.Configuration
{
    public interface IEventCorrelationBuilder
    {
        EventCorrelation Build();
    }
}
