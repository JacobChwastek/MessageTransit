namespace MessageTransit;

public interface PartitionKeyConsumeContext
{
    /// <summary>
    /// The partition key for the message (defaults to "")
    /// </summary>
    string? PartitionKey { get; }
}
