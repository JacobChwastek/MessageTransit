namespace MessageTransit;

using System.IO;
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
        TestEventHubs.Start(Path.Combine(TestContext.CurrentContext.TestDirectory, "config.json"));
    }
}
