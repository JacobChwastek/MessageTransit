namespace MessageTransit.Testing
{
    public interface IConsumerTestHarness<TConsumer>
        where TConsumer : class, IConsumer
    {
        IReceivedMessageList Consumed { get; }
    }
}
