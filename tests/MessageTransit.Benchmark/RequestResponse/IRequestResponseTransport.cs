namespace MessageTransitBenchmark.RequestResponse
{
    using System;
    using System.Threading.Tasks;
    using MessageTransit;


    public interface IRequestResponseTransport :
        IDisposable
    {
        Task<IRequestClient<T>> GetRequestClient<T>(TimeSpan settingsRequestTimeout)
            where T : class;

        void GetBusControl(Action<IReceiveEndpointConfigurator> callback);
    }
}
