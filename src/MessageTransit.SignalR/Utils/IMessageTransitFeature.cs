namespace MessageTransit.SignalR.Utils
{
    public interface IMessageTransitFeature
    {
        ConcurrentHashSet<string> Groups { get; }
    }
}
