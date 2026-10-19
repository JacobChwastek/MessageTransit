namespace MessageTransit.QuartzIntegration;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Context;
using Quartz;
using Serialization;


public class ScheduledMessageJob : IJob
{
    readonly IBus? _bus;

    /// <summary>
    /// Creates the job. The job sends through the bus that MessageTransit stores in the scheduler context when a bus configured
    /// with <c>UseInMemoryScheduler</c> starts, and through <paramref name="bus" /> when the scheduler context has no bus.
    /// </summary>
    /// <param name="bus">The bus used to send the scheduled message</param>
    public ScheduledMessageJob(IBus? bus = null)
    {
        _bus = bus;
    }

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        var jobData = context.MergedJobDataMap;
        var messageContext = new JobDataMessageContext(context, SystemTextJsonMessageSerializer.Instance);

        var contentType = new ContentType(jobData.GetString("ContentType")!);
        var destinationAddress = new Uri(jobData.GetString("Destination")!);
        var body = jobData.GetString("Body") ?? string.Empty;

        var supportedMessageTypes = (jobData.TryGetString("MessageType", out var text)
            ? text?.Split(';').ToArray()
            : default) ?? [];

        try
        {
            var bus = GetBus(context);

            var pipe = new ForwardScheduledMessagePipe(contentType, messageContext, body, destinationAddress, supportedMessageTypes);

            var endpoint = await bus.GetSendEndpoint(destinationAddress).ConfigureAwait(false);

            await endpoint.Send(new SerializedMessageBody(), pipe, cancellationToken).ConfigureAwait(false);

            LogContext.Debug?.Log("Schedule Executed: {Key} {Schedule}", context.Trigger.Key, context.Trigger.NextFireTimeUtc);
        }
        catch (Exception ex)
        {
            LogContext.Error?.Log(ex, "Failed to send scheduled message: {MessageType} {DestinationAddress}", supportedMessageTypes, destinationAddress);

            throw new JobExecutionException(ex) { RefireImmediately = context.RefireCount < 5 };
        }
    }

    IBus GetBus(IJobExecutionContext context)
    {
        // the bus that owns the scheduler wins over an injected bus, which a container resolves to its default bus
        if (context.Scheduler.Context.TryGetValue(QuartzConstants.BusContextKey, out var value) && value is IBus bus)
            return bus;

        if (_bus != null)
            return _bus;

        throw new InvalidOperationException("The bus is not available to the Quartz scheduler. Start the bus before the scheduler fires jobs, "
            + "or create the job with a job factory that supplies the bus.");
    }


    class ForwardScheduledMessagePipe : IPipe<SendContext>
    {
        readonly string _body;
        readonly ContentType? _contentType;
        readonly Uri? _destinationAddress;
        readonly string[] _supportedMessageTypes;
        readonly JobDataMessageContext _messageContext;

        public ForwardScheduledMessagePipe(ContentType? contentType, JobDataMessageContext messageContext, string body, Uri? destinationAddress,
            string[] supportedMessageTypes)
        {
            _contentType = contentType;
            _messageContext = messageContext;
            _body = body;
            _destinationAddress = destinationAddress;
            _supportedMessageTypes = supportedMessageTypes;
        }

        public Task Send(SendContext context)
        {
            var deserializer = context.Serialization.GetMessageDeserializer(_contentType);

            var body = deserializer.GetMessageBody(_body);

            var serializerContext = deserializer.Deserialize(body, _messageContext, _destinationAddress);

            if (_messageContext.MessageId.HasValue)
                context.MessageId = _messageContext.MessageId;

            context.RequestId = _messageContext.RequestId;
            context.ConversationId = _messageContext.ConversationId;
            context.CorrelationId = _messageContext.CorrelationId;
            context.InitiatorId = _messageContext.InitiatorId;
            context.SourceAddress = _messageContext.SourceAddress;
            context.ResponseAddress = _messageContext.ResponseAddress;
            context.FaultAddress = _messageContext.FaultAddress;

            if (_supportedMessageTypes.Any())
                context.SupportedMessageTypes = _supportedMessageTypes;

            if (_messageContext.ExpirationTime.HasValue)
                context.TimeToLive = _messageContext.ExpirationTime.Value.ToUniversalTime() - DateTime.UtcNow;

            foreach (KeyValuePair<string, object> header in _messageContext.Headers.GetAll())
                context.Headers.Set(header.Key, header.Value);

            IReadOnlyDictionary<string, object>? transportProperties = _messageContext.TransportProperties;
            if (transportProperties != null && context is TransportSendContext transportSendContext)
                transportSendContext.ReadPropertiesFrom(transportProperties);

            context.Serializer = serializerContext.GetMessageSerializer();

            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }
}
