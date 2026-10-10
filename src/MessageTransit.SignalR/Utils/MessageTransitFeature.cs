namespace MessageTransit.SignalR.Utils
{
    using System;


    public class MessageTransitFeature : IMessageTransitFeature
    {
        public ConcurrentHashSet<string> Groups { get; } = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
