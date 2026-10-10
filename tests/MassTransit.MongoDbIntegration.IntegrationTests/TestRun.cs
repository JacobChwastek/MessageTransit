using MassTransit.Testing.Containers;
using NUnit.Framework;


/// <summary>
/// Declared outside any namespace because this assembly also has fixtures outside MassTransit (LiberisLabs.*);
/// it runs before every fixture and removes the shared containers afterwards
/// </summary>
[SetUpFixture]
public class TestRun : ContainerTestRun
{
}
