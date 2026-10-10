namespace MessageTransit
{
    public interface IRabbitMqQueueBindingConfigurator :
        IRabbitMqQueueConfigurator,
        IRabbitMqExchangeBindingConfigurator
    {
    }
}
