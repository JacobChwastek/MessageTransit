namespace MessageTransit
{
    using Microsoft.Extensions.DependencyInjection;


    public interface ISagaRepositoryRegistrationConfigurator<TSaga> :
        IServiceCollection
        where TSaga : class, ISaga
    {
    }
}
