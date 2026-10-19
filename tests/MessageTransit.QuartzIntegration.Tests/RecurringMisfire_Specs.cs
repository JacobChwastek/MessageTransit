namespace MessageTransit.QuartzIntegration.Tests;

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Quartz;
using Scheduling;


[TestFixture]
public class A_recurring_schedule_with_an_adjustable_clock
{
    [Test]
    public async Task Should_fire_once_for_missed_events_when_the_policy_is_send()
    {
        await using var context = await ClockContext.Start();

        var schedule = new QuartzTestBus.HourlySchedule(context.ScheduleStart, MissedEventPolicy.Send);
        var firstFireTime = await context.Schedule(schedule);

        await context.AdvancePast(firstFireTime);

        Assert.That(await context.Bus.WaitForTick(TimeSpan.FromSeconds(10)), Is.True, "The missed events should fire once");

        await Task.Delay(TimeSpan.FromSeconds(2));

        Assert.That(context.Bus.ReceivedCount, Is.EqualTo(1), "Two missed events should fire only once");
    }

    [Test]
    public async Task Should_skip_missed_events_when_the_policy_is_skip()
    {
        await using var context = await ClockContext.Start();

        var schedule = new QuartzTestBus.HourlySchedule(context.ScheduleStart, MissedEventPolicy.Skip);
        var firstFireTime = await context.Schedule(schedule);

        await context.AdvancePast(firstFireTime);

        Assert.That(await context.Bus.WaitForTick(TimeSpan.FromSeconds(3)), Is.False, "The missed events should be skipped");

        var trigger = await context.Bus.WaitForTrigger(QuartzTestBus.RecurringTriggerKey(schedule));

        Assert.That(trigger.NextFireTimeUtc, Is.EqualTo(firstFireTime + TimeSpan.FromHours(2)));
    }

    [Test]
    public async Task Should_not_fire_while_paused_and_fire_the_missed_event_when_resumed()
    {
        await using var context = await ClockContext.Start();

        var schedule = new QuartzTestBus.HourlySchedule(context.ScheduleStart, MissedEventPolicy.Send);
        var firstFireTime = await context.Schedule(schedule);

        var endpoint = await context.Bus.GetSchedulerEndpoint();
        await endpoint.PauseScheduledRecurringSend(schedule.ScheduleId, schedule.ScheduleGroup);

        var scheduler = await context.Bus.GetScheduler();
        var triggerKey = QuartzTestBus.RecurringTriggerKey(schedule);

        await WaitForState(scheduler, triggerKey, TriggerState.Paused);

        await context.AdvancePast(firstFireTime);

        Assert.That(await context.Bus.WaitForTick(TimeSpan.FromSeconds(3)), Is.False, "A paused schedule should not fire");

        await endpoint.ResumeScheduledRecurringSend(schedule.ScheduleId, schedule.ScheduleGroup);

        Assert.That(await context.Bus.WaitForTick(TimeSpan.FromSeconds(10)), Is.True, "The resumed schedule should fire the missed event");
    }

    [Test]
    public async Task Should_not_fire_after_it_is_canceled()
    {
        await using var context = await ClockContext.Start();

        var schedule = new QuartzTestBus.HourlySchedule(context.ScheduleStart, MissedEventPolicy.Send);
        var firstFireTime = await context.Schedule(schedule);

        var endpoint = await context.Bus.GetSchedulerEndpoint();
        await endpoint.CancelScheduledRecurringSend(schedule.ScheduleId, schedule.ScheduleGroup);

        Assert.That(await context.Bus.WaitForTriggerRemoved(QuartzTestBus.RecurringTriggerKey(schedule)), Is.True);

        await context.AdvancePast(firstFireTime);

        Assert.That(await context.Bus.WaitForTick(TimeSpan.FromSeconds(3)), Is.False, "A canceled schedule should not fire");
    }

    [Test]
    public async Task Should_use_the_default_group_when_the_schedule_has_no_group()
    {
        await using var context = await ClockContext.Start();

        var schedule = new QuartzTestBus.HourlySchedule(context.ScheduleStart, MissedEventPolicy.Send, scheduleGroup: null);
        await context.Schedule(schedule);

        var triggerKey = new TriggerKey("Recurring.Trigger." + schedule.ScheduleId, TriggerKey.DefaultGroup);
        Assert.That(await (await context.Bus.GetScheduler()).Exists(triggerKey), Is.True);

        var endpoint = await context.Bus.GetSchedulerEndpoint();
        await endpoint.CancelScheduledRecurringSend(schedule.ScheduleId, null);

        Assert.That(await context.Bus.WaitForTriggerRemoved(triggerKey), Is.True);
    }

    static async Task WaitForState(IScheduler scheduler, TriggerKey triggerKey, TriggerState state)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            if (await scheduler.GetTriggerState(triggerKey) == state)
                return;

            await Task.Delay(50);
        }

        Assert.Fail($"The trigger did not reach the {state} state");
    }


    sealed class ClockContext : IAsyncDisposable
    {
        readonly QuartzTimeAdjustment _adjustment;
        readonly StandaloneSchedulerFactory _schedulerFactory;

        ClockContext(AdjustableTimeProvider clock, StandaloneSchedulerFactory schedulerFactory, QuartzTestBus bus)
        {
            Clock = clock;
            _schedulerFactory = schedulerFactory;
            Bus = bus;
            _adjustment = new QuartzTimeAdjustment(schedulerFactory);
        }

        public AdjustableTimeProvider Clock { get; }
        public QuartzTestBus Bus { get; }

        /// <summary>
        /// Starts schedules an hour out. Quartz picks up a trigger that is due within seconds before the test moves the clock, and a
        /// trigger it already picked up fires late instead of going through misfire handling.
        /// </summary>
        public DateTimeOffset ScheduleStart => Clock.GetUtcNow().AddHours(1);

        public async ValueTask DisposeAsync()
        {
            await Bus.DisposeAsync();
            _adjustment.Dispose();
            await _schedulerFactory.DisposeAsync();
        }

        public static async Task<ClockContext> Start()
        {
            var clock = new AdjustableTimeProvider();
            var schedulerFactory = QuartzSchedulerBuilder.Create(q => q.UseTimeProvider(clock)).Build();

            var bus = await QuartzTestBus.Start(schedulerFactory);

            return new ClockContext(clock, schedulerFactory, bus);
        }

        public async Task<DateTimeOffset> Schedule(RecurringSchedule schedule)
        {
            var endpoint = await Bus.GetSchedulerEndpoint();

            await endpoint.ScheduleRecurringSend(QuartzTestBus.InputAddress, schedule, new QuartzTestBus.Tick { Name = "hourly" });

            var trigger = await Bus.WaitForTrigger(QuartzTestBus.RecurringTriggerKey(schedule));

            return trigger.NextFireTimeUtc ?? throw new InvalidOperationException("The trigger has no next fire time");
        }

        /// <summary>
        /// Moves the clock to 90 minutes after the first fire time, so two hourly events are missed and the next one is 30 minutes away.
        /// </summary>
        public Task AdvancePast(DateTimeOffset firstFireTime)
        {
            return _adjustment.AdvanceTime(firstFireTime - Clock.GetUtcNow() + TimeSpan.FromMinutes(90));
        }
    }
}
