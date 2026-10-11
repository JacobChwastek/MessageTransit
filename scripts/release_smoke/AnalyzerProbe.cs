namespace ReleaseSmoke;

using MessageTransit;


// deliberately not awaited: the packaged analyzer must report MTA0001 for this call
public class AnalyzerProbe : IConsumer<OrderAccepted>
{
    public Task Consume(ConsumeContext<OrderAccepted> context)
    {
        context.Publish(new OrderAccepted(context.Message.OrderId));
        return Task.CompletedTask;
    }
}
