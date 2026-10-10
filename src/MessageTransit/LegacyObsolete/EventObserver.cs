namespace MessageTransit
{
}


namespace Automatonymous
{
    using System;
    using MessageTransit;


    [Obsolete("Deprecated, use IEventObserver instead")]
    public interface EventObserver<TSaga> :
        IEventObserver<TSaga>
        where TSaga : class, SagaStateMachineInstance
    {
    }
}
