namespace MessageTransit.Testing.Containers;

using System.Threading.Tasks;
using NUnit.Framework;


/// <summary>
/// Derive an assembly-level <see cref="SetUpFixtureAttribute" /> class from this to remove the shared containers after the run
/// </summary>
public abstract class ContainerTestRun
{
    [OneTimeTearDown]
    public Task Remove_containers()
    {
        return SharedContainers.DisposeAsync();
    }
}
