namespace MessageTransit.QuartzIntegration;

using Quartz;

static class QuartzConstants
{
    public const string RecurringTriggerPrefix = "Recurring.Trigger.";

    /// <summary>
    /// The scheduler context key that holds the bus that owns the scheduler. <see cref="ScheduledMessageJob" /> prefers it over a
    /// bus injected by a container.
    /// </summary>
    public const string BusContextKey = "MessageTransit.Bus";

    /// <summary>
    /// Quartz 4 refuses a null trigger group, which Quartz 3 treated as the default group.
    /// </summary>
    public static string GetScheduleGroup(string? scheduleGroup)
    {
        return scheduleGroup ?? TriggerKey.DefaultGroup;
    }
}
