namespace MessageTransit
{
    public interface RoutingKeySendContext
    {
        /// <summary>
        /// The routing key for the message (defaults to "")
        /// </summary>
        string? RoutingKey { get; set; }
    }
}
