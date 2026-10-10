namespace MessageTransit
{
    public interface IInMemoryBusTopology :
        IBusTopology
    {
        new IInMemoryMessagePublishTopology<T> Publish<T>()
            where T : class;
    }
}
