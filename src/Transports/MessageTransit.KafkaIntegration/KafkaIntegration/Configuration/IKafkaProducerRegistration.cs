namespace MessageTransit.KafkaIntegration.Configuration
{
    using MessageTransit.Configuration;


    public interface IKafkaProducerRegistration :
        IRegistration
    {
        void Register(IKafkaFactoryConfigurator configurator, IRiderRegistrationContext context);
    }
}
