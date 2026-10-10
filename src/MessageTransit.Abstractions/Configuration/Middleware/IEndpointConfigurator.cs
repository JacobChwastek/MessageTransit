namespace MessageTransit
{
    public interface IEndpointConfigurator :
        IConsumePipeConfigurator,
        ISendPipelineConfigurator,
        IPublishPipelineConfigurator,
        IReceivePipelineConfigurator
    {
    }
}
