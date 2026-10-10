namespace MessageTransit.Transactions
{
    using System.Threading.Tasks;


    public interface ITransactionalBus :
        IBus
    {
        Task Release();
    }
}
