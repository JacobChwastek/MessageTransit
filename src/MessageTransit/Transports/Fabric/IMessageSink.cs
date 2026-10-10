#nullable enable
namespace MessageTransit.Transports.Fabric
{
    using System.Threading.Tasks;


    public interface IMessageSink<T> :
        IProbeSite
        where T : class
    {
        Task Deliver(DeliveryContext<T> context);
    }
}
