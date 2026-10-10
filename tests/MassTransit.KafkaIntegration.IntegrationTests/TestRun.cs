namespace MassTransit;

using NUnit.Framework;


/// <summary>
/// Runs before every fixture in the MassTransit namespaces of this assembly and removes the shared containers afterwards
/// </summary>
[SetUpFixture]
public class TestRun : ContainerTestRun
{
    [OneTimeSetUp]
    public void Start_containers()
    {
        TestKafka.Start();
    }
}
