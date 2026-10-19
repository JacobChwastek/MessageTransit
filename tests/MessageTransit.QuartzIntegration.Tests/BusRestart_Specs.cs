namespace MessageTransit.QuartzIntegration.Tests;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Quartz;


[TestFixture]
public class A_restarted_bus_with_a_quartz_scheduler
{
    static readonly Uri RestartInputAddress = new("queue:restart-input");

    [Test]
    public async Task Should_send_scheduled_messages_with_the_default_scheduler()
    {
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ISchedulerFactory schedulerFactory = null;

        var bus = MessageTransit.Bus.Factory.CreateUsingInMemory(cfg =>
        {
            cfg.UseInMemoryScheduler(out schedulerFactory);

            ConfigureRestartInput(cfg, received);
        });

        var scheduler = await RestartAndSchedule(bus, schedulerFactory, received.Task);

        Assert.That(scheduler.Status, Is.EqualTo(SchedulerStatus.Shutdown), "The scheduler should shut down when the bus stops");
    }

    [Test]
    public async Task Should_send_scheduled_messages_with_a_supplied_scheduler_factory()
    {
        await using var schedulerFactory = QuartzSchedulerBuilder.Create().Build();

        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var bus = MessageTransit.Bus.Factory.CreateUsingInMemory(cfg =>
        {
            cfg.UseInMemoryScheduler(schedulerFactory);

            ConfigureRestartInput(cfg, received);
        });

        var scheduler = await RestartAndSchedule(bus, schedulerFactory, received.Task);

        Assert.That(scheduler.Status, Is.EqualTo(SchedulerStatus.Standby), "The scheduler should wait in standby until its factory is disposed");
    }

    [Test]
    public async Task Should_send_scheduled_messages_with_quartz_consumers()
    {
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var provider = new ServiceCollection()
            .AddQuartz()
            .AddMessageTransit(x =>
            {
                x.AddQuartzConsumers();

                x.UsingInMemory((context, cfg) =>
                {
                    ConfigureRestartInput(cfg, received);

                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);

        IScheduler scheduler;
        await using (provider)
        {
            scheduler = await RestartAndSchedule(provider.GetRequiredService<IBusControl>(), provider.GetRequiredService<ISchedulerFactory>(),
                received.Task);

            Assert.That(scheduler.Status, Is.EqualTo(SchedulerStatus.Standby), "The scheduler should wait in standby while the bus is stopped");
        }

        Assert.That(scheduler.Status, Is.EqualTo(SchedulerStatus.Shutdown), "The scheduler should shut down when the container is disposed");
    }

    [Test]
    public async Task Should_cancel_a_delayed_start_when_the_bus_stops()
    {
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startDelay = TimeSpan.FromSeconds(2);

        await using var provider = new ServiceCollection()
            .AddQuartz()
            .Configure<QuartzHostedServiceOptions>(options => options.StartDelay = startDelay)
            .AddMessageTransit(x =>
            {
                x.AddQuartzConsumers();
                x.UsingInMemory((context, cfg) =>
                {
                    ConfigureRestartInput(cfg, received);
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);

        var bus = provider.GetRequiredService<IBusControl>();
        var scheduler = await provider.GetRequiredService<ISchedulerFactory>().GetScheduler();

        try
        {
            await bus.StartAsync(TimeSpan.FromSeconds(30));
            await bus.StopAsync(TimeSpan.FromSeconds(30));

            Assert.That(scheduler.Status, Is.EqualTo(SchedulerStatus.Created), "The bus should stop before the delayed start");

            await Task.Delay(startDelay + TimeSpan.FromSeconds(1));

            Assert.That(scheduler.Status, Is.EqualTo(SchedulerStatus.Created), "The canceled delay must not start a stopped bus's scheduler");

            await bus.StartAsync(TimeSpan.FromSeconds(30));

            var messageScheduler = bus.CreateMessageScheduler(QuartzTestBus.SchedulerAddress);
            await messageScheduler.ScheduleSend(RestartInputAddress, DateTime.UtcNow, new QuartzTestBus.Tick { Name = "delayed-restart" });

            await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            await bus.StopAsync(TimeSpan.FromSeconds(30));
        }
    }

    static void ConfigureRestartInput(IInMemoryBusFactoryConfigurator configurator, TaskCompletionSource received)
    {
        configurator.ReceiveEndpoint("restart-input", e => e.Handler<QuartzTestBus.Tick>(_ =>
        {
            received.TrySetResult();
            return Task.CompletedTask;
        }));
    }

    /// <summary>
    /// Starts, stops and starts the bus again, schedules a message and waits for it, then stops the bus. Returns the scheduler that
    /// sent the message.
    /// </summary>
    static async Task<IScheduler> RestartAndSchedule(IBusControl bus, ISchedulerFactory schedulerFactory, Task received)
    {
        await bus.StartAsync(TimeSpan.FromSeconds(30));
        await bus.StopAsync(TimeSpan.FromSeconds(30));

        await bus.StartAsync(TimeSpan.FromSeconds(30));
        try
        {
            var messageScheduler = bus.CreateMessageScheduler(QuartzTestBus.SchedulerAddress);
            await messageScheduler.ScheduleSend(RestartInputAddress, DateTime.UtcNow, new QuartzTestBus.Tick { Name = "restart" });

            var completed = await Task.WhenAny(received, Task.Delay(TimeSpan.FromSeconds(10)));

            Assert.That(completed, Is.SameAs(received), "The scheduled message should arrive after the bus restarts");

            return await schedulerFactory.GetScheduler();
        }
        finally
        {
            await bus.StopAsync(TimeSpan.FromSeconds(30));
        }
    }
}
