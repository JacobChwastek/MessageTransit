namespace MessageTransit.QuartzIntegration.Tests;

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using Quartz;
using Scheduling;


[TestFixture]
public class Restarting_the_scheduler_with_a_durable_store
{
    [Test]
    public async Task Should_send_a_message_that_was_due_while_the_scheduler_was_stopped()
    {
        ScheduledMessage<QuartzTestBus.Tick> scheduled;

        await using (var factory = CreateSchedulerFactory())
        await using (var bus = await QuartzTestBus.Start(factory, startScheduler: false))
        {
            // Persist an already overdue trigger while execution is disabled. No race with scheduler shutdown.
            var scheduledTime = DateTime.UtcNow - TimeSpan.FromMinutes(1);

            var scheduler = bus.Bus.CreateMessageScheduler(QuartzTestBus.SchedulerAddress);
            scheduled = await scheduler.ScheduleSend(QuartzTestBus.InputAddress, scheduledTime, new QuartzTestBus.Tick { Name = "durable" });

            await bus.WaitForTrigger(new TriggerKey(scheduled.TokenId.ToString("N")));

            await bus.Stop();

            Assert.That(bus.ReceivedCount, Is.Zero, "The message should not be sent before the scheduler stops");
        }

        await using (var factory = CreateSchedulerFactory())
        await using (var bus = await QuartzTestBus.Start(factory))
        {
            Assert.That(await bus.WaitForTick(TimeSpan.FromSeconds(30)), Is.True, "The restarted scheduler should send the missed message");

            Assert.That(await bus.WaitForTriggerRemoved(new TriggerKey(scheduled.TokenId.ToString("N"))), Is.True);
        }
    }

    [Test]
    public async Task Should_keep_a_recurring_schedule_that_can_be_canceled_after_the_restart()
    {
        var schedule = new QuartzTestBus.HourlySchedule(DateTimeOffset.UtcNow.AddDays(1), MissedEventPolicy.Skip);
        var triggerKey = QuartzTestBus.RecurringTriggerKey(schedule);
        DateTimeOffset? nextFireTime;

        await using (var factory = CreateSchedulerFactory())
        await using (var bus = await QuartzTestBus.Start(factory, startScheduler: false))
        {
            var endpoint = await bus.GetSchedulerEndpoint();
            await endpoint.ScheduleRecurringSend(QuartzTestBus.InputAddress, schedule, new QuartzTestBus.Tick { Name = "recurring" });

            nextFireTime = (await bus.WaitForTrigger(triggerKey)).NextFireTimeUtc;
        }

        await using (var factory = CreateSchedulerFactory())
        await using (var bus = await QuartzTestBus.Start(factory))
        {
            var trigger = await bus.WaitForTrigger(triggerKey);

            Assert.Multiple(() =>
            {
                Assert.That(trigger, Is.InstanceOf<ICronTrigger>());
                Assert.That(((ICronTrigger)trigger).CronExpressionString, Is.EqualTo(schedule.CronExpression));
                Assert.That(trigger.NextFireTimeUtc, Is.EqualTo(nextFireTime));
            });

            var endpoint = await bus.GetSchedulerEndpoint();
            await endpoint.CancelScheduledRecurringSend(schedule.ScheduleId, schedule.ScheduleGroup);

            Assert.That(await bus.WaitForTriggerRemoved(triggerKey), Is.True, "The recurring schedule should be canceled");
        }
    }

    string _databasePath;

    [SetUp]
    public void CreateDatabase()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"messagetransit-quartz-{NewId.NextGuid():N}.db");
    }

    [TearDown]
    public void DeleteDatabase()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
    }

    StandaloneSchedulerFactory CreateSchedulerFactory()
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString();

        return QuartzSchedulerBuilder.Create(q =>
            {
                q.ConfigureScheduler(options => options.InstanceName = "MessageTransitDurableTests");
                q.UsePersistentStore(store =>
                {
                    store.UseSqlite(SqliteFactory.Instance, connectionString);
                    store.UseSystemTextJsonSerializer();
                    store.ProvisionSchema();
                    store.ConfigureStore(options => options.MisfireThreshold = TimeSpan.FromSeconds(1));
                });
            })
            .Build();
    }
}
