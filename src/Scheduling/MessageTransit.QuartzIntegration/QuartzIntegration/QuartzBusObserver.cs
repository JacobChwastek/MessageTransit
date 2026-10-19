namespace MessageTransit.QuartzIntegration;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Quartz;


/// <summary>
/// Used by container-based Quartz configurations, to start Quartz with the bus and put it in standby when the bus stops. Quartz 4
/// can't start a scheduler again after it shuts down, so the scheduler shuts down when the container disposes this observer.
/// </summary>
public class QuartzBusObserver : IBusObserver, IAsyncDisposable, IDisposable
{
    readonly IOptions<QuartzHostedServiceOptions> _options;
    readonly ISchedulerFactory _schedulerFactory;
    CancellationTokenSource? _delayedStartCancellation;
    Task _delayedStartTask = Task.CompletedTask;
    IScheduler? _scheduler;

    public QuartzBusObserver(ISchedulerFactory schedulerFactory, IOptions<QuartzHostedServiceOptions> options)
    {
        _schedulerFactory = schedulerFactory;
        _options = options;
    }

    public void PostCreate(IBus bus)
    {
    }

    public void CreateFaulted(Exception exception)
    {
    }

    public Task PreStart(IBus bus)
    {
        return Task.CompletedTask;
    }

    public async Task PostStart(IBus bus, Task<BusReady> busReady)
    {
        await busReady.ConfigureAwait(false);

        _scheduler = await _schedulerFactory.GetScheduler().ConfigureAwait(false);

        if (_options.Value.StartDelay is { } startDelay)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(startDelay, TimeSpan.Zero);

            _delayedStartCancellation = new CancellationTokenSource();
            var cancellationToken = _delayedStartCancellation.Token;
            var delay = Task.Delay(startDelay, _scheduler.TimeProvider, cancellationToken);

            _delayedStartTask = StartAfterDelay(_scheduler, delay, cancellationToken);
        }
        else
            await _scheduler.Start().ConfigureAwait(false);
    }

    public Task StartFaulted(IBus bus, Exception exception)
    {
        return CancelDelayedStart();
    }

    public async Task PreStop(IBus bus)
    {
        await CancelDelayedStart().ConfigureAwait(false);

        if (_scheduler is { Status: not (SchedulerStatus.ShuttingDown or SchedulerStatus.Shutdown) })
            await _scheduler.Standby().ConfigureAwait(false);
    }

    public Task PostStop(IBus bus)
    {
        return Task.CompletedTask;
    }

    public Task StopFaulted(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await CancelDelayedStart().ConfigureAwait(false);

        var scheduler = _scheduler;
        if (scheduler == null || scheduler.Status is SchedulerStatus.ShuttingDown or SchedulerStatus.Shutdown)
            return;

        try
        {
            await scheduler.Shutdown(_options.Value.WaitForJobsToComplete).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogContext.Warning?.Log(exception, "Quartz Scheduler shutdown faulted: {Name}/{InstanceId}", scheduler.SchedulerName,
                scheduler.SchedulerInstanceId);
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    async Task CancelDelayedStart()
    {
        var cancellation = Interlocked.Exchange(ref _delayedStartCancellation, null);
        try
        {
            cancellation?.Cancel();

            // A start already in progress must finish before standby or shutdown, or it could start the scheduler again afterward.
            await _delayedStartTask.ConfigureAwait(false);
        }
        finally
        {
            cancellation?.Dispose();
        }
    }

    static async Task StartAfterDelay(IScheduler scheduler, Task delay, CancellationToken cancellationToken)
    {
        try
        {
            await delay.ConfigureAwait(false);
            await scheduler.Start(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            LogContext.Warning?.Log(exception, "Quartz Scheduler delayed start faulted: {Name}/{InstanceId}", scheduler.SchedulerName,
                scheduler.SchedulerInstanceId);
        }
    }
}
