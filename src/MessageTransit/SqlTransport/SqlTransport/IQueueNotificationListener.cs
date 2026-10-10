namespace MessageTransit.SqlTransport
{
    using System.Threading.Tasks;


    public interface IQueueNotificationListener
    {
        Task MessageReady(string queueName);
    }
}
