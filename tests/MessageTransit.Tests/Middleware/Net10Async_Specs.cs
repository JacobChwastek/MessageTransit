namespace MessageTransit.Tests.Middleware;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MessageTransit.Internals;
using MessageTransit.Metadata;
using NUnit.Framework;
using MessageTransit.Testing;
using MessageTransit.Util;


[TestFixture]
public class Net10Async_Specs
{
    [Test]
    public async Task Should_dispose_the_source_after_taking_the_requested_messages()
    {
        var disposed = false;
        var messages = await Messages(() => disposed = true).Take(2).ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(messages, Is.EqualTo(new[] { "one", "two" }));
            Assert.That(disposed, Is.True);
        });
    }

    [Test]
    public async Task Should_not_enumerate_when_taking_zero_messages()
    {
        var disposed = false;
        var messages = await Messages(() => disposed = true).Take(0).ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(messages, Is.Empty);
            Assert.That(disposed, Is.False);
        });
    }

    [Test]
    public void Should_forward_enumeration_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var exception = Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await Messages(() => { }).Take(2).ToListAsync(cancellation.Token));

        Assert.That(exception.CancellationToken, Is.EqualTo(cancellation.Token));
    }

    [Test]
    public async Task Should_cancel_a_wait_with_the_original_token_without_canceling_the_operation()
    {
        using var cancellation = new CancellationTokenSource();
        var source = TaskUtil.GetTask<string>();
        var wait = source.Task.OrCanceled(cancellation.Token);
        cancellation.Cancel();

        var exception = Assert.ThrowsAsync<OperationCanceledException>(async () => await wait);
        Assert.Multiple(() =>
        {
            Assert.That(exception.CancellationToken, Is.EqualTo(cancellation.Token));
            Assert.That(source.Task.IsCompleted, Is.False);
        });

        source.SetResult("completed later");
        Assert.That(await source.Task, Is.EqualTo("completed later"));
    }

    [Test]
    public async Task Should_return_the_original_task_when_cancellation_is_disabled()
    {
        var source = TaskUtil.GetTask<string>();
        Assert.That(source.Task.OrCanceled(CancellationToken.None), Is.SameAs(source.Task));
        source.SetResult("done");
        Assert.That(await source.Task, Is.EqualTo("done"));
    }

    [Test]
    public void Should_preserve_fault_identity_and_cached_task_results()
    {
        var expected = new InvalidOperationException("expected");
        var fault = TaskUtil.Faulted<string>(expected);
        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await fault);
        var defaultTask = TaskUtil.Default<string>();
        var secondDefaultTask = TaskUtil.Default<string>();
        var canceledTask = TaskUtil.Cancelled<string>();
        var secondCanceledTask = TaskUtil.Cancelled<string>();

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.SameAs(expected));
            Assert.That(TaskUtil.Completed.IsCompletedSuccessfully, Is.True);
            Assert.That(defaultTask, Is.SameAs(secondDefaultTask));
            Assert.That(canceledTask, Is.SameAs(secondCanceledTask));
            Assert.That(canceledTask.IsCanceled, Is.True);
            Assert.That(TaskUtil.True.Result, Is.True);
            Assert.That(TaskUtil.False.Result, Is.False);
            Assert.That(HostMetadataCache.IsNetFramework, Is.False);
        });
    }

    static async IAsyncEnumerable<string> Messages(Action disposed, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return "one";
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return "two";
            yield return "three";
        }
        finally
        {
            disposed();
        }
    }
}
