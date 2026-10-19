namespace MessageTransit.QuartzIntegration;

using System;
using System.Threading;


/// <summary>
/// A clock that runs at the speed of another clock, shifted by an offset that can be changed while the scheduler runs. Give it to
/// the scheduler with <c>UseTimeProvider</c> so that <see cref="QuartzTimeAdjustment" /> can move the scheduler's time.
/// </summary>
public sealed class AdjustableTimeProvider : TimeProvider
{
    readonly TimeProvider _inner;
    long _offsetTicks;

    public AdjustableTimeProvider() : this(System)
    {
    }

    public AdjustableTimeProvider(TimeProvider inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <summary>
    /// The amount added to the time of the inner clock.
    /// </summary>
    public TimeSpan Offset
    {
        get => TimeSpan.FromTicks(Interlocked.Read(ref _offsetTicks));
        set => Interlocked.Exchange(ref _offsetTicks, value.Ticks);
    }

    public override TimeZoneInfo LocalTimeZone => _inner.LocalTimeZone;

    public override long TimestampFrequency => _inner.TimestampFrequency;

    public override DateTimeOffset GetUtcNow()
    {
        return _inner.GetUtcNow() + Offset;
    }

    public override long GetTimestamp()
    {
        return _inner.GetTimestamp();
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        return _inner.CreateTimer(callback, state, dueTime, period);
    }

    internal void Advance(TimeSpan duration)
    {
        Interlocked.Add(ref _offsetTicks, duration.Ticks);
    }
}
