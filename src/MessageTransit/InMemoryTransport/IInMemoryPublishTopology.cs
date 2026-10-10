namespace MessageTransit
{
    public interface IInMemoryPublishTopology :
        IPublishTopology
    {
        new IInMemoryMessagePublishTopology<T> GetMessageTopology<T>()
            where T : class;
    }
}
