namespace MessageTransit.QuartzIntegration;

using System;
using System.Threading.Tasks;
using Quartz;
using QuartzSchedulerOptions = QuartzSchedulerOptions;


/// <summary>
/// Used to start and stop an in-memory scheduler using Quartz
/// </summary>
public class SchedulerBusObserver : IBusObserver
{
    readonly QuartzSchedulerOptions _options;
    readonly Uri _schedulerEndpointAddress;
    IScheduler? _scheduler;

    /// <summary>
    /// Creates the bus observer to initialize the Quartz scheduler.
    /// </summary>
    /// <param name="options">Configuration to initialize with.</param>
    public SchedulerBusObserver(QuartzSchedulerOptions options)
    {
        _options = options;
        _schedulerEndpointAddress = new Uri($"queue:{options.QueueName}");
    }

    public void PostCreate(IBus bus)
    {
    }

    public void CreateFaulted(Exception exception)
    {
    }

    public async Task PreStart(IBus bus)
    {
        LogContext.Debug?.Log("Creating Quartz Scheduler: {InputAddress}", _schedulerEndpointAddress);

        _scheduler = await _options.SchedulerFactory.GetScheduler().ConfigureAwait(false);

        _scheduler.Context[QuartzConstants.BusContextKey] = bus;
    }

    public async Task PostStart(IBus bus, Task<BusReady> busReady)
    {
        if (!_options.StartScheduler)
        {
            LogContext.Debug?.Log("Quartz Scheduler: {InputAddress} ({Name}/{InstanceId}) initialized, but not started",
                _schedulerEndpointAddress,
                _scheduler?.SchedulerName,
                _scheduler?.SchedulerInstanceId);

            return;
        }

        LogContext.Debug?.Log("Quartz Scheduler Starting: {InputAddress} ({Name}/{InstanceId})", _schedulerEndpointAddress, _scheduler?.SchedulerName,
            _scheduler?.SchedulerInstanceId);

        await busReady.ConfigureAwait(false);

        await _scheduler!.Start().ConfigureAwait(false);

        LogContext.Debug?.Log("Quartz Scheduler Started: {InputAddress} ({Name}/{InstanceId})", _schedulerEndpointAddress, _scheduler.SchedulerName,
            _scheduler.SchedulerInstanceId);
    }

    public Task StartFaulted(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }

    public async Task PreStop(IBus bus)
    {
        if (!_options.StartScheduler || _scheduler == null || _scheduler.Status is SchedulerStatus.ShuttingDown or SchedulerStatus.Shutdown)
            return;

        await _scheduler.Standby().ConfigureAwait(false);

        LogContext.Debug?.Log("Quartz Scheduler Paused: {InputAddress} ({Name}/{InstanceId})", _schedulerEndpointAddress, _scheduler.SchedulerName,
            _scheduler.SchedulerInstanceId);
    }

    public async Task PostStop(IBus bus)
    {
        if (_scheduler == null)
            return;

        // Quartz 4 can't start a scheduler again after it shuts down. A supplied factory's scheduler stays in standby so the bus can
        // start it again, and its owner shuts it down. The default factory builds a new scheduler on the next start.
        if (_options.SchedulerFactory is not RebuildingSchedulerFactory)
            return;

        await _scheduler.Shutdown().ConfigureAwait(false);

        LogContext.Debug?.Log("Quartz Scheduler Stopped: {InputAddress} ({Name}/{InstanceId})", _schedulerEndpointAddress, _scheduler.SchedulerName,
            _scheduler.SchedulerInstanceId);
    }

    public Task StopFaulted(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }
}
