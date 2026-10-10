#nullable enable
namespace MessageTransit
{
    /// <summary>
    /// Configures the topic subscription for the receive endpoint
    /// </summary>
    public interface ISqlTopicSubscriptionConfigurator :
        ISqlTopicConfigurator
    {
        SqlSubscriptionType SubscriptionType { set; }

        string? RoutingKey { set; }
    }
}
