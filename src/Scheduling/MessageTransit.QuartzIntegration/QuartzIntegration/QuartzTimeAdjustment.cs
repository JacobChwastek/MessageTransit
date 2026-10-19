namespace MessageTransit.QuartzIntegration;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Quartz;


/// <summary>
/// Moves the time of a Quartz scheduler forward, so that tests can fire scheduled messages without waiting. The scheduler must be
/// built with an <see cref="AdjustableTimeProvider" />, for example <c>q.UseTimeProvider(new AdjustableTimeProvider())</c>.
/// </summary>
public class QuartzTimeAdjustment : IDisposable
{
    readonly ISchedulerFactory _schedulerFactory;
    AdjustableTimeProvider? _timeProvider;
    long _advancedTicks;

    public QuartzTimeAdjustment(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    public QuartzTimeAdjustment(IServiceProvider provider)
    {
        _schedulerFactory = provider.GetRequiredService<ISchedulerFactory>();
    }

    /// <summary>
    /// Moves the scheduler's clock back by the total amount this adjustment advanced it.
    /// </summary>
    public void Dispose()
    {
        var advanced = Interlocked.Exchange(ref _advancedTicks, 0);
        if (advanced != 0)
            _timeProvider?.Advance(TimeSpan.FromTicks(-advanced));
    }

    public async Task AdvanceTime(TimeSpan duration)
    {
        var scheduler = await _schedulerFactory.GetScheduler().ConfigureAwait(false);

        if (scheduler.TimeProvider is not AdjustableTimeProvider timeProvider)
        {
            throw new InvalidOperationException($"The Quartz scheduler '{scheduler.SchedulerName}' does not use an {nameof(AdjustableTimeProvider)}. "
                + $"Build the scheduler with UseTimeProvider(new {nameof(AdjustableTimeProvider)}()) to adjust its time.");
        }

        _timeProvider = timeProvider;

        await scheduler.Standby().ConfigureAwait(false);

        timeProvider.Advance(duration);
        Interlocked.Add(ref _advancedTicks, duration.Ticks);

        await scheduler.Start().ConfigureAwait(false);
    }
}
