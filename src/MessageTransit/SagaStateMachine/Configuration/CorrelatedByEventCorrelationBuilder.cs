namespace MessageTransit.Configuration
{
    using System;


    public class CorrelatedByEventCorrelationBuilder<TInstance, TData> :
        IEventCorrelationBuilder
        where TData : class, CorrelatedBy<Guid>
        where TInstance : class, SagaStateMachineInstance
    {
        readonly StateMachineInterfaceType<TInstance, TData>.MessageTransitEventCorrelationConfigurator _configurator;

        public CorrelatedByEventCorrelationBuilder(SagaStateMachine<TInstance> machine, Event<TData> @event)
        {
            var configurator = new StateMachineInterfaceType<TInstance, TData>.MessageTransitEventCorrelationConfigurator(machine, @event, null);
            configurator.CorrelateById(x => x.Message.CorrelationId);

            _configurator = configurator;
        }

        public EventCorrelation Build()
        {
            return _configurator.Build();
        }
    }
}
