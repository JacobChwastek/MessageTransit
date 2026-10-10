namespace MessageTransit.DependencyInjection.Registration
{
    using Microsoft.Extensions.DependencyInjection;


    public interface CachedRegistration
    {
        void Register(IServiceCollection collection);
    }
}
