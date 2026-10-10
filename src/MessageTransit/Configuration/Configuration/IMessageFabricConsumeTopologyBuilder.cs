namespace MessageTransit.Configuration
{
    public interface IMessageFabricConsumeTopologyBuilder :
        IMessageFabricTopologyBuilder
    {
        string Exchange { get; set; }
        string Queue { get; set; }
    }
}
