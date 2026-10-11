using MessageTransit.Testing.Containers;
using NUnit.Framework;


/// <summary>
/// Declared outside any namespace because this assembly also has fixtures outside MessageTransit (LiberisLabs.*);
/// it runs before every fixture and removes the shared containers afterwards
/// </summary>
[SetUpFixture]
public class TestRun : ContainerTestRun
{
    /// <summary>
    /// Starts MongoDB before any test harness runs, so a slow first start does not count against a harness inactivity timeout
    /// </summary>
    [OneTimeSetUp]
    public void Start_containers()
    {
        TestMongo.Server.Start();
    }
}
