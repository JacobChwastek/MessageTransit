namespace MessageTransit.EventHubIntegration.Tests.Contracts
{
    public interface BatchEventHubMessage
    {
        int Index { get; }
    }
}
