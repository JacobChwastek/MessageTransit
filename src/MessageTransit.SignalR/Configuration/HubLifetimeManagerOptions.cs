namespace MessageTransit.SignalR
{
    using System;
    using Microsoft.AspNetCore.SignalR;
    using Utils;


    public class HubLifetimeManagerOptions<THub> :
        IHubLifetimeManagerOptions<THub>
        where THub : Hub
    {
        public HubLifetimeManagerOptions()
        {
            ServerName = $"{Environment.MachineName}_{NewId.NextGuid():N}";
            RequestTimeout = TimeSpan.FromSeconds(20);
            ConnectionStore = new HubConnectionStore();
            GroupsSubscriptionManager = new MessageTransitSubscriptionManager();
            UsersSubscriptionManager = new MessageTransitSubscriptionManager();
        }

        public HubConnectionStore ConnectionStore { get; }
        public MessageTransitSubscriptionManager GroupsSubscriptionManager { get; }
        public MessageTransitSubscriptionManager UsersSubscriptionManager { get; }

        public string ServerName { get; set; }
        public RequestTimeout RequestTimeout { get; set; }
    }
}
