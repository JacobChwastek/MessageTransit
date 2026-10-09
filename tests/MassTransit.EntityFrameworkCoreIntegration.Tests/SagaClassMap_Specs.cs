namespace MassTransit.EntityFrameworkCoreIntegration.Tests;

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NUnit.Framework;


[TestFixture]
public class SagaClassMap_Specs
{
    [Test]
    public void Should_preserve_the_application_assigned_correlation_key()
    {
        var model = new ModelBuilder();
        new DefaultMap().Configure(model);

        var entity = model.Model.FindEntityType(typeof(SagaState));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entity.FindPrimaryKey().Properties, Has.Count.EqualTo(1));
            Assert.That(entity.FindPrimaryKey().Properties[0].Name, Is.EqualTo(nameof(SagaState.CorrelationId)));
            Assert.That(entity.FindProperty(nameof(SagaState.CorrelationId)).ValueGenerated, Is.EqualTo(ValueGenerated.Never));
        }
    }

    [Test]
    public void Should_apply_the_correlation_key_customization_before_entity_configuration()
    {
        var model = new ModelBuilder();
        var map = new CustomKeyMap();

        map.Configure(model);

        var key = model.Model.FindEntityType(typeof(SagaState)).FindPrimaryKey();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(key.GetName(), Is.EqualTo("PK_CustomSaga"));
            Assert.That(key.IsClustered(), Is.False);
            Assert.That(map.CustomKeyVisibleInEntityConfiguration, Is.True);
        }
    }


    class SagaState : ISaga
    {
        public Guid CorrelationId { get; set; }
    }


    class DefaultMap : SagaClassMap<SagaState>
    {
    }


    class CustomKeyMap : SagaClassMap<SagaState>
    {
        public bool CustomKeyVisibleInEntityConfiguration { get; private set; }

        protected override KeyBuilder ConfigureCorrelationIdKey(KeyBuilder keyBuilder)
        {
            return keyBuilder.HasName("PK_CustomSaga").IsClustered(false);
        }

        protected override void Configure(EntityTypeBuilder<SagaState> entity, ModelBuilder model)
        {
            CustomKeyVisibleInEntityConfiguration = entity.Metadata.FindPrimaryKey().GetName() == "PK_CustomSaga";
        }
    }
}
