namespace ReleaseSmoke;

using MessageTransit;


public record SubmitOrder(Guid OrderId, string Description);


public record OrderAccepted(Guid OrderId);


public class SubmitOrderConsumer : IConsumer<SubmitOrder>
{
    public static TaskCompletionSource<(Guid Id, string Description, string? ContentType, string Serializer, Uri InputAddress)> Receipt { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Consume(ConsumeContext<SubmitOrder> context)
    {
        Receipt.TrySetResult((context.Message.OrderId, context.Message.Description, context.ReceiveContext.ContentType.MediaType,
            context.SerializerContext.GetType().FullName!, context.ReceiveContext.InputAddress));
        return context.Publish(new OrderAccepted(context.Message.OrderId));
    }
}


public class OrderState : SagaStateMachineInstance
{
    public string CurrentState { get; set; } = null!;
    public Guid CorrelationId { get; set; }
}


public class OrderStateMachine : MessageTransitStateMachine<OrderState>
{
    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);
        Event(() => Submitted, x => x.CorrelateById(m => m.Message.OrderId));
        Initially(When(Submitted).TransitionTo(Accepted));
    }

    public State Accepted { get; private set; } = null!;
    public Event<SubmitOrder> Submitted { get; private set; } = null!;
}
