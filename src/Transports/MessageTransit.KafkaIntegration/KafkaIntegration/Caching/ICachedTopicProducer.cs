namespace MessageTransit.KafkaIntegration.Caching
{
    using System;
    using MessageTransit.Caching;


    public interface ICachedTopicProducer<out T> :
        INotifyValueUsed,
        IAsyncDisposable
    {
        T Key { get; }
    }
}
