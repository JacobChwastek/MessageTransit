namespace MessageTransit
{
    public interface ReceiveEndpointStopping :
        ReceiveEndpointEvent
    {
        bool Removed { get; }
    }
}
