namespace MessageTransit
{
    public interface ITransportSequenceNumber
    {
        ulong? SequenceNumber { get; }
    }
}
