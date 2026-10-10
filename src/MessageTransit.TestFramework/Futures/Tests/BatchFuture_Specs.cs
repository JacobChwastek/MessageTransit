namespace MessageTransit.TestFramework.Futures.Tests;

using NUnit.Framework;


[TestFixture]
public class BatchFuture_Specs :
    FutureTestFixture
{
    public BatchFuture_Specs(IFutureTestFixtureConfigurator testFixtureConfigurator)
        : base(testFixtureConfigurator)
    {
    }

    protected override void ConfigureMessageTransit(IBusRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<ProcessBatchItemConsumer>();
        configurator.AddFuture<BatchFuture>();
    }
}
