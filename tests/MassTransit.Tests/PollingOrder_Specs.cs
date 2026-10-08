namespace MassTransit.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Util;


[TestFixture]
public class PollingOrder_Specs
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task Should_dispatch_results_in_order_before_their_processing_completes(bool grouped)
    {
        const int count = 10;
        using var algorithm = new RequestRateAlgorithm(new RequestRateAlgorithmOptions
        {
            PrefetchCount = count,
            RequestResultLimit = count,
            ConcurrentResultLimit = count
        });

        var sync = new object();
        var accepted = new List<int>();
        var allAccepted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var complete = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        int[] acceptedBeforeEnumerationFinished = [];

        IEnumerable<int> ObserveEnumeration(IEnumerable<int> values)
        {
            // Independently scheduled callbacks cannot enter this lock until enumeration ends.
            // Direct callbacks enter it reentrantly, even while earlier processing is incomplete.
            lock (sync)
            {
                foreach (var value in values)
                    yield return value;

                acceptedBeforeEnumerationFinished = accepted.ToArray();
            }
        }

        Task<IEnumerable<int>> Receive(int limit, CancellationToken cancellationToken)
        {
            return Task.FromResult(grouped
                ? Enumerable.Range(0, count).Reverse()
                : ObserveEnumeration(Enumerable.Range(0, count)));
        }

        Task Process(int value, CancellationToken cancellationToken)
        {
            lock (sync)
            {
                accepted.Add(value);
                if (accepted.Count == count)
                    allAccepted.TrySetResult(true);
            }

            return complete.Task;
        }

        try
        {
            var dispatch = grouped
                ? algorithm.Run(Receive, Process, values => values.GroupBy(_ => 0), values => ObserveEnumeration(values.OrderBy(x => x)))
                : algorithm.Run(Receive, Process);

            await dispatch.WaitAsync(TimeSpan.FromSeconds(10));
            await allAccepted.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Multiple(() =>
            {
                Assert.That(acceptedBeforeEnumerationFinished, Is.EqualTo(Enumerable.Range(0, count)));
                Assert.That(complete.Task.IsCompleted, Is.False);
            });
        }
        finally
        {
            complete.TrySetResult(true);

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (await algorithm.Run((available, _) => Task.FromResult(available), timeout.Token) != count)
                await Task.Yield();
        }
    }
}
