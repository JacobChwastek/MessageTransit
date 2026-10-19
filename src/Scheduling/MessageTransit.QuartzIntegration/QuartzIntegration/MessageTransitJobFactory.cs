namespace MessageTransit.QuartzIntegration;

using System;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using Quartz.Extensibility;


/// <summary>
/// Creates <see cref="ScheduledMessageJob" /> instances that send through the specified bus.
/// </summary>
public class MessageTransitJobFactory : IJobFactory
{
    readonly IBus _bus;

    public MessageTransitJobFactory(IBus bus)
    {
        _bus = bus;
    }

    public ValueTask<JobScope> CreateJob(TriggerFiredBundle bundle, IScheduler scheduler, CancellationToken cancellationToken = default)
    {
        return new ValueTask<JobScope>(new JobScope(new ScheduledMessageJob(_bus)));
    }

    public ValueTask ReturnJob(JobScope scope, CancellationToken cancellationToken = default)
    {
        return scope.Job switch
        {
            IAsyncDisposable asyncDisposable => asyncDisposable.DisposeAsync(),
            IDisposable disposable => Dispose(disposable),
            _ => default
        };
    }

    static ValueTask Dispose(IDisposable disposable)
    {
        disposable.Dispose();
        return default;
    }
}
