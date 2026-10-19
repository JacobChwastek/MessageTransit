namespace MessageTransit.QuartzIntegration.Tests;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using Scheduling;


/// <summary>
/// An in-memory bus that runs a Quartz scheduler and records the <see cref="QuartzTestBus.Tick" /> messages it receives.
/// </summary>
sealed class QuartzTestBus : IAsyncDisposable
{
    public static readonly Uri InputAddress = new("queue:quartz-input");
    public static readonly Uri SchedulerAddress = new("queue:quartz");

    readonly IBusControl _bus;
    readonly ConcurrentQueue<Tick> _received = new();
    readonly SemaphoreSlim _signal = new(0);
    bool _stopped;

    QuartzTestBus(ISchedulerFactory schedulerFactory, bool startScheduler)
    {
        SchedulerFactory = schedulerFactory;

        _bus = MessageTransit.Bus.Factory.CreateUsingInMemory(cfg =>
        {
            cfg.UseInMemoryScheduler(options =>
            {
                options.SchedulerFactory = schedulerFactory;
                options.StartScheduler = startScheduler;
            });

            cfg.ReceiveEndpoint("quartz-input", e => e.Handler<Tick>(context =>
            {
                _received.Enqueue(context.Message);
                _signal.Release();
                return Task.CompletedTask;
            }));
        });
    }

    public ISchedulerFactory SchedulerFactory { get; }

    public IBus Bus => _bus;

    public int ReceivedCount => _received.Count;

    public async ValueTask DisposeAsync()
    {
        await Stop();
        _signal.Dispose();
    }

    public static async Task<QuartzTestBus> Start(ISchedulerFactory schedulerFactory, bool startScheduler = true)
    {
        var bus = new QuartzTestBus(schedulerFactory, startScheduler);

        await bus._bus.StartAsync(TimeSpan.FromSeconds(30));

        return bus;
    }

    public async Task Stop()
    {
        if (_stopped)
            return;

        _stopped = true;
        await _bus.StopAsync(TimeSpan.FromSeconds(30));
    }

    public Task<ISendEndpoint> GetSchedulerEndpoint()
    {
        return _bus.GetSendEndpoint(SchedulerAddress);
    }

    public async Task<bool> WaitForTick(TimeSpan timeout)
    {
        return await _signal.WaitAsync(timeout);
    }

    public async Task<IScheduler> GetScheduler()
    {
        return await SchedulerFactory.GetScheduler();
    }

    /// <summary>
    /// Waits until the scheduler holds the trigger, since the schedule message is consumed asynchronously.
    /// </summary>
    public async Task<ITrigger> WaitForTrigger(TriggerKey triggerKey)
    {
        var scheduler = await GetScheduler();

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            var trigger = await scheduler.GetTrigger(triggerKey);
            if (trigger != null)
                return trigger;

            await Task.Delay(50);
        }

        throw new TimeoutException($"The trigger was not scheduled: {triggerKey}");
    }

    /// <summary>
    /// Waits until the scheduler no longer holds the trigger.
    /// </summary>
    public async Task<bool> WaitForTriggerRemoved(TriggerKey triggerKey)
    {
        var scheduler = await GetScheduler();

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            if (!await scheduler.Exists(triggerKey))
                return true;

            await Task.Delay(50);
        }

        return false;
    }

    public static TriggerKey RecurringTriggerKey(RecurringSchedule schedule)
    {
        return new TriggerKey("Recurring.Trigger." + schedule.ScheduleId, schedule.ScheduleGroup ?? TriggerKey.DefaultGroup);
    }


    public class Tick
    {
        public string Name { get; set; }
    }


    public class HourlySchedule : RecurringSchedule
    {
        public HourlySchedule(DateTimeOffset startTime, MissedEventPolicy misfirePolicy, string scheduleGroup = "quartz-tests")
        {
            StartTime = startTime;
            MisfirePolicy = misfirePolicy;
            ScheduleId = NewId.NextGuid().ToString("N");
            ScheduleGroup = scheduleGroup;
        }

        public string TimeZoneId => "UTC";
        public DateTimeOffset StartTime { get; }
        public DateTimeOffset? EndTime => null;
        public string ScheduleId { get; }
        public string ScheduleGroup { get; }
        public string CronExpression => "0 0 * * * ?";
        public string Description => "Hourly test schedule";
        public MissedEventPolicy MisfirePolicy { get; }
    }
}
