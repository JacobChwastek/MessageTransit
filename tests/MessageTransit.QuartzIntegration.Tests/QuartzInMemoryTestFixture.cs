namespace MessageTransit.QuartzIntegration.Tests;

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Quartz;
using Scheduling;
using TestFramework;


public abstract class QuartzInMemoryTestFixture : InMemoryTestFixture
{
    readonly Lazy<IMessageScheduler> _messageScheduler;
    QuartzTimeAdjustment _adjustment;
    StandaloneSchedulerFactory _schedulerFactory;

    protected QuartzInMemoryTestFixture()
    {
        QuartzAddress = new Uri("loopback://localhost/quartz");

        _messageScheduler = new Lazy<IMessageScheduler>(() =>
            new MessageScheduler(new EndpointScheduleMessageProvider(() => GetSendEndpoint(QuartzAddress)), Bus.Topology));
    }

    protected Uri QuartzAddress { get; }

    protected ISendEndpoint QuartzEndpoint { get; set; }

    protected IMessageScheduler Scheduler => _messageScheduler.Value;

    protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
    {
        _schedulerFactory = QuartzSchedulerBuilder.Create(q => q.UseTimeProvider(new AdjustableTimeProvider())).Build();

        configurator.UseInMemoryScheduler(_schedulerFactory);

        _adjustment = new QuartzTimeAdjustment(_schedulerFactory);

        base.ConfigureInMemoryBus(configurator);
    }

    protected Task AdvanceTime(TimeSpan duration)
    {
        return _adjustment.AdvanceTime(duration);
    }

    [OneTimeSetUp]
    public async Task Setup_quartz_service()
    {
        QuartzEndpoint = await GetSendEndpoint(QuartzAddress);
    }

    [OneTimeTearDown]
    public async Task Take_it_down()
    {
        // Stop the bus before its scheduler shuts down. The base fixture's stop that runs next has nothing left to do.
        await InMemoryTestHarness.Stop();

        _adjustment?.Dispose();

        if (_schedulerFactory != null)
            await _schedulerFactory.DisposeAsync();
    }
}
