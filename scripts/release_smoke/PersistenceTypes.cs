namespace ReleaseSmoke;

using MessageTransit;
using MessageTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PersistenceState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = "";
    public string Description { get; set; } = "";
}

public class SmokeMap : SagaClassMap<PersistenceState>
{
    protected override void Configure(EntityTypeBuilder<PersistenceState> entity, ModelBuilder model)
    {
        entity.Property(x => x.CurrentState).HasMaxLength(64);
        entity.Property(x => x.Description).HasMaxLength(128);
    }
}

public class SmokeDbContext : SagaDbContext
{
    public SmokeDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new SmokeMap(); }
    }
}
