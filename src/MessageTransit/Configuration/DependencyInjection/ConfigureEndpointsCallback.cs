namespace MessageTransit
{
    public delegate void ConfigureEndpointsCallback(string queueName, IReceiveEndpointConfigurator configurator);
}
