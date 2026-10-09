namespace MassTransit.EntityFrameworkCoreIntegration.Tests.AuditStore;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Audit;
using MassTransit.Audit;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Serialization;
using Shared;


[TestFixture(typeof(SqlServerTestDbParameters))]
[TestFixture(typeof(SqlServerResiliencyTestDbParameters))]
[TestFixture(typeof(PostgresTestDbParameters))]
public class AuditRecordParity_Specs<T> where T : ITestDbParameters, new()
{
    DbContextOptions _options;

    [OneTimeSetUp]
    public async Task Create_database()
    {
        _options = new T().GetDbContextOptions<AuditDbContext>().Options;
        await using var context = new AuditDbContext(_options, "audit_records", "audit_schema");
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    [OneTimeTearDown]
    public async Task Delete_database()
    {
        await using var context = new AuditDbContext(_options, "audit_records", "audit_schema");
        await context.Database.EnsureDeletedAsync();
    }

    [TestCase("Send")]
    [TestCase("Consume")]
    public async Task Should_round_trip_message_metadata_and_json(string contextType)
    {
        var metadata = new MessageAuditMetadata
        {
            ContextType = contextType,
            MessageId = NewId.NextGuid(),
            ConversationId = NewId.NextGuid(),
            CorrelationId = NewId.NextGuid(),
            InitiatorId = NewId.NextGuid(),
            RequestId = NewId.NextGuid(),
            SentTime = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            SourceAddress = "loopback://localhost/source",
            DestinationAddress = "loopback://localhost/destination",
            InputAddress = "loopback://localhost/input",
            ResponseAddress = "loopback://localhost/response",
            FaultAddress = "loopback://localhost/fault",
            Headers = new Dictionary<string, string> { ["tenant"] = "north" },
            Custom = new Dictionary<string, string> { ["application"] = "audit-tests" }
        };
        var message = new AuditMessage { Value = "stored value" };
        IMessageAuditStore store = new EntityFrameworkAuditStore(_options, "audit_records", "audit_schema");

        await store.StoreMessage(message, metadata);

        await using var context = new AuditDbContext(_options, "audit_records", "audit_schema");
        var record = await context.Set<AuditRecord>().SingleAsync(x => x.MessageId == metadata.MessageId);
        var body = ObjectDeserializer.Deserialize<AuditMessage>(record.Message);

        Assert.Multiple(() =>
        {
            Assert.That(record.AuditRecordId, Is.GreaterThan(0));
            Assert.That(record.ContextType, Is.EqualTo(metadata.ContextType));
            Assert.That(record.MessageId, Is.EqualTo(metadata.MessageId));
            Assert.That(record.ConversationId, Is.EqualTo(metadata.ConversationId));
            Assert.That(record.CorrelationId, Is.EqualTo(metadata.CorrelationId));
            Assert.That(record.InitiatorId, Is.EqualTo(metadata.InitiatorId));
            Assert.That(record.RequestId, Is.EqualTo(metadata.RequestId));
            Assert.That(record.SentTime, Is.EqualTo(metadata.SentTime));
            Assert.That(record.SourceAddress, Is.EqualTo(metadata.SourceAddress));
            Assert.That(record.DestinationAddress, Is.EqualTo(metadata.DestinationAddress));
            Assert.That(record.InputAddress, Is.EqualTo(metadata.InputAddress));
            Assert.That(record.ResponseAddress, Is.EqualTo(metadata.ResponseAddress));
            Assert.That(record.FaultAddress, Is.EqualTo(metadata.FaultAddress));
            Assert.That(record.MessageType, Does.Contain(nameof(AuditMessage)));
            Assert.That(record.Headers, Is.EquivalentTo(metadata.Headers));
            Assert.That(record.Custom, Is.EquivalentTo(metadata.Custom));
            Assert.That(body.Value, Is.EqualTo(message.Value));
        });
    }


    public class AuditMessage
    {
        public string Value { get; set; }
    }
}
