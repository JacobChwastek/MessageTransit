namespace MessageTransit.QuartzIntegration.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Quartz;


[TestFixture]
public class A_secondary_bus_with_a_container_scheduler
{
    [Test]
    public async Task Should_send_scheduled_messages_through_the_bus_that_owns_the_scheduler()
    {
        var received = new TaskCompletionSource<QuartzTestBus.Tick>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var provider = new ServiceCollection()
            .AddQuartz()
            .AddMessageTransit(x => x.UsingInMemory())
            .AddMessageTransit<ISecondBus>(x =>
            {
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseInMemoryScheduler(context.GetRequiredService<ISchedulerFactory>());

                    cfg.ReceiveEndpoint("scheduled-input", e => e.Handler<QuartzTestBus.Tick>(context =>
                    {
                        received.TrySetResult(context.Message);
                        return Task.CompletedTask;
                    }));
                });
            })
            .BuildServiceProvider(true);

        var depot = provider.GetRequiredService<IBusDepot>();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await depot.Start(timeout.Token);
        try
        {
            var secondBus = provider.GetRequiredService<ISecondBus>();

            var scheduler = secondBus.CreateMessageScheduler(QuartzTestBus.SchedulerAddress);
            await scheduler.ScheduleSend(new Uri("queue:scheduled-input"), DateTime.UtcNow, new QuartzTestBus.Tick { Name = "secondary" });

            var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(10)));

            Assert.That(completed, Is.SameAs(received.Task), "The scheduled message should reach the consumer on the secondary bus");
        }
        finally
        {
            await depot.Stop(CancellationToken.None);
        }
    }


    public interface ISecondBus : IBus
    {
    }
}
