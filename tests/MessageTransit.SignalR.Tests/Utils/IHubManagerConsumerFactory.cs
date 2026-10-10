namespace MessageTransit.SignalR.Tests.Utils
{
    using Microsoft.AspNetCore.SignalR;


    public interface IHubManagerConsumerFactory<THub>
        where THub : Hub
    {
        MessageTransitHubLifetimeManager<THub> HubLifetimeManager { get; set; }
    }
}
