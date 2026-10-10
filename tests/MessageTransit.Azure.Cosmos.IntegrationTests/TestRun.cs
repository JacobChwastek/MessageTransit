namespace MessageTransit;

using NUnit.Framework;


/// <summary>
/// Runs before every fixture in the MessageTransit namespaces of this assembly and removes the shared containers afterwards
/// </summary>
[SetUpFixture]
public class TestRun : ContainerTestRun
{
    [OneTimeSetUp]
    public void Start_containers()
    {
        if (Azure.Cosmos.Tests.Configuration.UsesEmulator)
            TestCosmos.Emulator.Start();
    }
}
