using MessageTransit;
using MessageTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using ReleaseSmoke;
#if RABBITMQ
using Testcontainers.RabbitMq;

await using var broker = new RabbitMqBuilder("rabbitmq:4.1-alpine").Build();
await broker.StartAsync();
#endif


await using var provider = new ServiceCollection()
    .AddMessageTransitTestHarness(x =>
    {
        x.AddConsumer<SubmitOrderConsumer>();
        x.AddSagaStateMachine<OrderStateMachine, OrderState>().InMemoryRepository();
#if RABBITMQ
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(broker.Hostname, broker.GetMappedPublicPort(5672), "/", h =>
            {
                h.Username(RabbitMqBuilder.DefaultUsername);
                h.Password(RabbitMqBuilder.DefaultPassword);
            });
            cfg.ConfigureEndpoints(context);
        });
#else
        x.UsingInMemory((context, cfg) =>
        {
#if NEWTONSOFT
            cfg.UseNewtonsoftJsonSerializer();
#elif MESSAGEPACK
            cfg.UseMessagePackSerializer();
#endif
            cfg.ConfigureEndpoints(context);
        });
#endif
    })
    .BuildServiceProvider(true);

var harness = provider.GetRequiredService<ITestHarness>();
await harness.Start();

var orderId = NewId.NextGuid();
await harness.Bus.Publish(new SubmitOrder(orderId, "package consumer round trip"));
var receipt = await SubmitOrderConsumer.Receipt.Task.WaitAsync(TimeSpan.FromSeconds(30));

var checks = new Dictionary<string, bool>
{
    ["consumer consumed SubmitOrder"] = await harness.Consumed.Any<SubmitOrder>(),
    ["saga created for the order"] = await harness.GetSagaStateMachineHarness<OrderStateMachine, OrderState>().Created.Any(x => x.CorrelationId == orderId),
    ["saga reached Accepted state"] = await harness.GetSagaStateMachineHarness<OrderStateMachine, OrderState>()
        .Exists(orderId, machine => machine.Accepted, TimeSpan.FromSeconds(30)) == orderId,
    ["consumer published OrderAccepted"] = await harness.Published.Any<OrderAccepted>(),
    ["application URN unchanged"] = MessageUrn.ForTypeString<SubmitOrder>() == "urn:message:ReleaseSmoke:SubmitOrder",
    ["framework URN uses MessageTransit"] = MessageUrn.ForTypeString<Fault<SubmitOrder>>() == "urn:message:MessageTransit:Fault[[ReleaseSmoke:SubmitOrder]]",
    ["message payload round trip"] = receipt.Id == orderId && receipt.Description == "package consumer round trip",
#if NEWTONSOFT
    ["Newtonsoft serializer used"] = receipt.ContentType == "application/vnd.messagetransit+json"
        && receipt.Serializer == "MessageTransit.Serialization.NewtonsoftJsonSerializerContext",
#elif MESSAGEPACK
    ["MessagePack serializer used"] = receipt.ContentType == "application/vnd.messagetransit+msgpack"
        && receipt.Serializer == "MessageTransit.Serialization.MessagePackMessageSerializerContext",
#elif RABBITMQ
    ["RabbitMQ transport used"] = receipt.InputAddress.Scheme == "rabbitmq",
#endif
};

foreach (var (check, passed) in checks)
    Console.WriteLine($"{(passed ? "pass" : "FAIL")}\t{check}");

await harness.Stop();

return checks.Values.All(passed => passed) ? 0 : 1;
