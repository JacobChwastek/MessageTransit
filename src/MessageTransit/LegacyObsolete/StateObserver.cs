namespace MessageTransit
{
}


namespace Automatonymous
{
    using System;
    using MessageTransit;


    [Obsolete("Deprecated, use IStateObserver instead")]
    public interface StateObserver<TSaga> :
        IStateObserver<TSaga>
        where TSaga : class, SagaStateMachineInstance
    {
    }
}
