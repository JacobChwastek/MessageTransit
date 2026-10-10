namespace MessageTransitBenchmark.BusOutbox;

using System.Collections.Generic;
using MessageTransit;
using MessageTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;


public class BusOutboxDbContext :
    SagaDbContext
{
    public BusOutboxDbContext(DbContextOptions<BusOutboxDbContext> options)
        : base(options)
    {
    }

    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield break; }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddTransactionalOutboxEntities();
    }
}
