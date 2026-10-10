namespace MessageTransit.Configuration
{
    public interface IConsumePipeConfiguration
    {
        IConsumePipeSpecification Specification { get; }
        IConsumePipeConfigurator Configurator { get; }
    }
}
