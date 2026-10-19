namespace MessageTransit;

using System;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using QuartzIntegration;
using Scheduling;
using Util;


public static class QuartzIntegrationExtensions
{
    [Obsolete("Use the new .AddQuartzConsumers() method, combined with AddQuartz(), to configure the Quartz scheduler")]
    public static Uri UseInMemoryScheduler(this IBusFactoryConfigurator configurator, IBusRegistrationContext context, string queueName = "quartz")
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        if (context == null)
            throw new ArgumentNullException(nameof(context));

        return configurator.UseInMemoryScheduler(options =>
        {
            options.SchedulerFactory = context.GetService<ISchedulerFactory>() ?? CreateDefaultSchedulerFactory();
            options.QueueName = queueName;
        });
    }

    public static Uri UseInMemoryScheduler(this IBusFactoryConfigurator configurator, string queueName = "quartz")
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        return configurator.UseInMemoryScheduler(CreateDefaultSchedulerFactory(), queueName);
    }

    public static Uri UseInMemoryScheduler(this IBusFactoryConfigurator configurator, out ISchedulerFactory schedulerFactory, string queueName = "quartz")
    {
        schedulerFactory = CreateDefaultSchedulerFactory();

        return UseInMemoryScheduler(configurator, schedulerFactory, queueName);
    }

    /// <summary>
    /// Use the specified Quartz scheduler factory for the in-memory scheduler. The scheduler starts with the bus and goes into standby
    /// when the bus stops, so the bus can start again. The bus doesn't shut it down: dispose the factory, or shut the scheduler down,
    /// when the application no longer needs it.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="schedulerFactory">The scheduler factory, for example one built with <see cref="QuartzSchedulerBuilder" /></param>
    /// <param name="queueName">The scheduler queue name</param>
    public static Uri UseInMemoryScheduler(this IBusFactoryConfigurator configurator, ISchedulerFactory schedulerFactory, string queueName = "quartz")
    {
        return configurator.UseInMemoryScheduler(options =>
        {
            options.SchedulerFactory = schedulerFactory;
            options.QueueName = queueName;
        });
    }

    static ISchedulerFactory CreateDefaultSchedulerFactory()
    {
        var instanceName = $"MessageTransit-{NewId.Next().ToString(FormatUtil.Formatter)}";

        return new RebuildingSchedulerFactory(() => QuartzSchedulerBuilder.Create(q =>
            {
                q.ConfigureScheduler(options => options.InstanceName = instanceName);
                q.UseDefaultThreadPool(Environment.ProcessorCount);
            })
            .Build());
    }

    public static Uri UseInMemoryScheduler(this IBusFactoryConfigurator configurator, Action<QuartzSchedulerOptions>? configure)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var options = new QuartzSchedulerOptions();
        configure?.Invoke(options);

        Uri? inputAddress = null;

        var observer = new SchedulerBusObserver(options);

        configurator.ReceiveEndpoint(options.QueueName, e =>
        {
            var partitioner = configurator.CreatePartitioner(Environment.ProcessorCount);

            e.Consumer(() => new ScheduleMessageConsumer(options.SchedulerFactory), x =>
                x.Message<ScheduleMessage>(m => m.UsePartitioner(partitioner, p => p.Message.CorrelationId)));

            e.Consumer(() => new CancelScheduledMessageConsumer(options.SchedulerFactory), x =>
                x.Message<CancelScheduledMessage>(m => m.UsePartitioner(partitioner, p => p.Message.TokenId)));

            e.Consumer(() => new PauseScheduledMessageConsumer(options.SchedulerFactory));

            e.Consumer(() => new ResumeScheduledMessageConsumer(options.SchedulerFactory));

            configurator.UseMessageScheduler(e.InputAddress);

            configurator.ConnectBusObserver(observer);

            inputAddress = e.InputAddress;
        });

        return inputAddress!;
    }
}
