namespace MessageTransit;

using System;
using Quartz;
using QuartzIntegration;


public class QuartzSchedulerOptions
{
    ISchedulerFactory? _schedulerFactory;

    /// <summary>
    /// Used to create the scheduler at bus start. Defaults to a factory built by <see cref="QuartzSchedulerBuilder" /> with the
    /// default Quartz configuration, whose scheduler shuts down when the bus stops and is built again when the bus starts again.
    /// The scheduler from a factory you supply goes into standby when the bus stops, and the bus doesn't shut it down.
    /// </summary>
    public ISchedulerFactory SchedulerFactory
    {
        get => _schedulerFactory ??= new RebuildingSchedulerFactory(() => QuartzSchedulerBuilder.Create().Build());
        set => _schedulerFactory = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// The queue name for the quartz service, defaults to "quartz".
    /// </summary>
    public string QueueName { get; set; } = "quartz";

    /// <summary>
    /// Whether to start the scheduler when bus starts, defaults to true.
    /// </summary>
    public bool StartScheduler { get; set; } = true;
}
