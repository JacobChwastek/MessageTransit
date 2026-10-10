namespace MassTransit.EntityFrameworkCoreIntegration.Tests;

using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NUnit.Framework;
using Saga;
using Shared;


[TestFixture]
public class OptimisticLoadCancellation_Specs
{
    [TestCase(false)]
    [TestCase(true)]
    public void Should_forward_cancellation_to_the_database(bool customizeQuery)
    {
        using var cancellation = new CancellationTokenSource();
        var interceptor = new CaptureConnectionCancellation();
        var options = new DbContextOptionsBuilder<LoadContext>()
            .UseOfflineSqlServer()
            .AddInterceptors(interceptor)
            .Options;
        using var context = new LoadContext(options);
        var executor = customizeQuery
            ? new OptimisticLoadQueryExecutor<SagaState>(query => query)
            : new OptimisticLoadQueryExecutor<SagaState>();

        Assert.ThrowsAsync<ConnectionObservedException>(async () => await executor.Load(context, NewId.NextGuid(), cancellation.Token));

        Assert.That(interceptor.CancellationToken, Is.EqualTo(cancellation.Token));
    }


    class CaptureConnectionCancellation : DbConnectionInterceptor
    {
        public CancellationToken CancellationToken { get; private set; }

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
            throw new ConnectionObservedException();
        }
    }


    class ConnectionObservedException : Exception
    {
    }


    class SagaState : ISaga
    {
        public Guid CorrelationId { get; set; }
    }


    class LoadContext : DbContext
    {
        public LoadContext(DbContextOptions<LoadContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SagaState>().HasKey(x => x.CorrelationId);
        }
    }
}
