namespace ReleaseSmoke;

using MessageTransit;
using MessageTransit.TestFramework;
using NUnit.Framework;


public record Ping(Guid Id);


[TestFixture]
public class Packaged_test_framework :
    InMemoryTestFixture
{
    [Test]
    public async Task Should_consume_a_published_message()
    {
        Task<ConsumeContext<Ping>> consumed = await ConnectPublishHandler<Ping>();

        var id = NewId.NextGuid();
        await Bus.Publish(new Ping(id));

        var context = await consumed;
        Assert.That(context.Message.Id, Is.EqualTo(id));
    }

    /// <summary>
    /// Fails on purpose: the release check expects this awaited assertion to fail the test run
    /// </summary>
    [Test]
    public async Task Should_fail_when_an_awaited_async_assertion_fails()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.CompletedTask);
    }
}
