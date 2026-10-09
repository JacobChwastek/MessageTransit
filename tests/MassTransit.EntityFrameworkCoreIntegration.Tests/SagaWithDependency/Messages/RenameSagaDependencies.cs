namespace MassTransit.EntityFrameworkCoreIntegration.Tests.SagaWithDependency.Messages;

/// <summary>
/// Correlated by saga name, so the repository loads the matching sagas through a query
/// </summary>
public class RenameSagaDependencies
{
    public string SagaName { get; set; }
    public string Name { get; set; }
}
