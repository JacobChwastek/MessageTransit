namespace MessageTransit.ActiveMqTransport.Tests;

using System;


internal static class ActiveMqBusFactoryConfiguratorExtensions
{
    public static void ConfigureHost(this IActiveMqBusFactoryConfigurator configurator, string testFlavor)
    {
        if (testFlavor == "artemis")
        {
            TestActiveMq.Artemis.Start();
            configurator.Host("localhost", TestActiveMq.ArtemisPort, cfgHost =>
            {
                cfgHost.Username("admin");
                cfgHost.Password("admin");
            });
            configurator.EnableArtemisCompatibility();
        }
        else if (testFlavor == ActiveMqHostAddress.AmqpScheme)
        {
            configurator.Host(new Uri("amqp://localhost:5672"), cfgHost =>
            {
                cfgHost.Username("admin");
                cfgHost.Password("admin");
            });
        }
    }
}
