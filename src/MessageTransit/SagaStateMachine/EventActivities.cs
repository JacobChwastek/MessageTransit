namespace MessageTransit
{
    using System.Collections.Generic;
    using SagaStateMachine;


    public interface EventActivities<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        IEnumerable<IActivityBinder<TInstance>> GetStateActivityBinders();
    }
}
