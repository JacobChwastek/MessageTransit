namespace MessageTransit.QuartzIntegration;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Quartz;


/// <summary>
/// The scheduler factory MessageTransit creates when the application does not supply one. Quartz 4 can't start a scheduler again
/// after it shuts down, so once the bus stops and shuts the scheduler down, the next request builds a new one, which lets the bus
/// start again.
/// </summary>
sealed class RebuildingSchedulerFactory : ISchedulerFactory
{
    readonly Func<StandaloneSchedulerFactory> _createFactory;
    readonly SemaphoreSlim _lock = new(1, 1);
    StandaloneSchedulerFactory? _factory;
    IScheduler? _scheduler;

    public RebuildingSchedulerFactory(Func<StandaloneSchedulerFactory> createFactory)
    {
        _createFactory = createFactory;
    }

    public async ValueTask<IScheduler> GetScheduler(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_factory != null && _scheduler is { Status: SchedulerStatus.ShuttingDown or SchedulerStatus.Shutdown })
            {
                await _factory.DisposeAsync().ConfigureAwait(false);

                _factory = null;
                _scheduler = null;
            }

            _factory ??= _createFactory();

            return _scheduler ??= await _factory.GetScheduler(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask<List<IScheduler>> GetAllSchedulers(CancellationToken cancellationToken = default)
    {
        return [await GetScheduler(cancellationToken).ConfigureAwait(false)];
    }

    public async ValueTask<IScheduler?> LookupScheduler(string schedulerName, CancellationToken cancellationToken = default)
    {
        var scheduler = await GetScheduler(cancellationToken).ConfigureAwait(false);

        return string.Equals(scheduler.SchedulerName, schedulerName, StringComparison.OrdinalIgnoreCase) ? scheduler : null;
    }
}
