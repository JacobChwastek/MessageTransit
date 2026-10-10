namespace MessageTransit.Monitoring
{
    using Metadata;
    using Microsoft.Extensions.Options;


    public class ConfigureDefaultInstrumentationOptions :
        IConfigureOptions<InstrumentationOptions>
    {
        public void Configure(InstrumentationOptions options)
        {
            options.ServiceName = HostMetadataCache.Host.ProcessName;
            options.EndpointLabel = "messaging.messagetransit.destination";
            options.ConsumerTypeLabel = "messaging.messagetransit.consumer_type";
            options.ExceptionTypeLabel = "messaging.messagetransit.exception_type";
            options.MessageTypeLabel = "messaging.messagetransit.message_type";
            options.ActivityNameLabel = "messaging.messagetransit.activity_type";
            options.ArgumentTypeLabel = "messaging.messagetransit.argument_type";
            options.LogTypeLabel = "messaging.messagetransit.log_type";
            options.ServiceNameLabel = "messaging.messagetransit.service";
            options.ReceiveTotal = "messaging.messagetransit.receive";
            options.ReceiveFaultTotal = "messaging.messagetransit.receive.errors";
            options.ReceiveDuration = "messaging.messagetransit.receive.duration";
            options.ReceiveInProgress = "messaging.messagetransit.receive.active";
            options.ConsumeTotal = "messaging.messagetransit.consume";
            options.ConsumeFaultTotal = "messaging.messagetransit.consume.errors";
            options.ConsumeRetryTotal = "messaging.messagetransit.consume.retries";
            options.ConsumeDuration = "messaging.messagetransit.consume.duration";
            options.ConsumerInProgress = "messaging.messagetransit.consume.active";
            options.SagaTotal = "messaging.messagetransit.saga";
            options.SagaFaultTotal = "messaging.messagetransit.saga.errors";
            options.SagaDuration = "messaging.messagetransit.saga.duration";
            options.HandlerTotal = "messaging.messagetransit.handler";
            options.HandlerFaultTotal = "messaging.messagetransit.handler.errors";
            options.HandlerDuration = "messaging.messagetransit.handler.duration";
            options.OutboxDeliveryTotal = "messaging.messagetransit.outbox.delivery";
            options.OutboxDeliveryFaultTotal = "messaging.messagetransit.outbox.delivery.errors";
            options.DeliveryDuration = "messaging.messagetransit.delivery.duration";
            options.SendTotal = "messaging.messagetransit.send";
            options.SendFaultTotal = "messaging.messagetransit.send.errors";
            options.OutboxSendTotal = "messaging.messagetransit.outbox.send";
            options.OutboxSendFaultTotal = "messaging.messagetransit.outbox.send.errors";
            options.ActivityExecuteTotal = "messaging.messagetransit.execute";
            options.ActivityExecuteFaultTotal = "messaging.messagetransit.execute.errors";
            options.ActivityExecuteDuration = "messaging.messagetransit.execute.duration";
            options.ExecuteInProgress = "messaging.messagetransit.execute.active";
            options.ActivityCompensateTotal = "messaging.messagetransit.compensate";
            options.ActivityCompensateFailureTotal = "messaging.messagetransit.compensate.errors";
            options.ActivityCompensateDuration = "messaging.messagetransit.compensate.duration";
            options.CompensateInProgress = "messaging.messagetransit.compensate.active";
            options.BusInstances = "messaging.messagetransit.bus";
            options.EndpointInstances = "messaging.messagetransit.endpoint";
            options.HandlerInProgress = "messaging.messagetransit.handler.active";
            options.SagaInProgress = "messaging.messagetransit.saga.active";
        }
    }
}
