namespace MessageTransit.Configuration
{
    public interface IStateMachineInterfaceType
    {
        ISagaMessageConnector<T> GetConnector<T>()
            where T : class, ISaga;
    }
}
