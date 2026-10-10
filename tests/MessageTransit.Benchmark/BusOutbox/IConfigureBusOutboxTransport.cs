namespace MessageTransitBenchmark.BusOutbox;

using System;
using MessageTransit;


public interface IConfigureBusOutboxTransport
{
    void Using(IBusRegistrationConfigurator configurator, Action<IBusRegistrationContext, IBusFactoryConfigurator> callback);
}
