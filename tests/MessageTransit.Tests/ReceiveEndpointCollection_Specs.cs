namespace MessageTransit.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;
using InMemoryTransport;
using MessageTransit.InMemoryTransport.Configuration;
using Logging;
using MessageTransit.Transports;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;


[TestFixture]
public class ReceiveEndpointCollection_Specs
{
    static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

    [TestCase(false)]
    [TestCase(true)]
    public async Task Should_stop_completed_endpoints(bool isBusEndpoint)
    {
        var transport = new RecordingReceiveTransport("completed_endpoint", isBusEndpoint);
        var endpoints = new ReceiveEndpointCollection();
        endpoints.Add("completed_endpoint", transport.Endpoint);

        var handles = endpoints.StartEndpoints(CancellationToken.None);
        await transport.Context.TransportObservers.NotifyReady(transport.Context.InputAddress);
        await handles[0].Ready.WaitAsync(TestTimeout);

        await transport.Context.TransportObservers.NotifyCompleted(transport.Context.InputAddress, transport);
        Assert.That(transport.Endpoint.CurrentState, Is.EqualTo(ReceiveEndpoint.State.Completed));

        await endpoints.StopEndpoints(CancellationToken.None).WaitAsync(TestTimeout);
        await endpoints.StopEndpoints(CancellationToken.None).WaitAsync(TestTimeout);

        Assert.That(transport.StopCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Should_finish_stopping_receive_endpoints_before_stopping_the_bus_endpoint()
    {
        var releaseReceiveStop = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var receiveTransport = new RecordingReceiveTransport("input_endpoint", false, releaseReceiveStop.Task);
        var busTransport = new RecordingReceiveTransport("bus_endpoint", true);
        var endpoints = new ReceiveEndpointCollection();
        endpoints.Add("bus_endpoint", busTransport.Endpoint);
        endpoints.Add("input_endpoint", receiveTransport.Endpoint);
        endpoints.StartEndpoints(CancellationToken.None);

        var stop = endpoints.StopEndpoints(CancellationToken.None);
        try
        {
            await receiveTransport.StopStarted.Task.WaitAsync(TestTimeout);
            Assert.That(busTransport.StopCount, Is.Zero);
        }
        finally
        {
            releaseReceiveStop.TrySetResult(true);
            await stop.WaitAsync(TestTimeout);
        }

        Assert.Multiple(() =>
        {
            Assert.That(receiveTransport.StopCount, Is.EqualTo(1));
            Assert.That(busTransport.StopCount, Is.EqualTo(1));
        });
    }


    class RecordingReceiveTransport : IReceiveTransport, ReceiveTransportHandle, DeliveryMetrics
    {
        readonly Task _stopCompletion;

        public RecordingReceiveTransport(string endpointName, bool isBusEndpoint, Task stopCompletion = null)
        {
            var topology = new InMemoryTopologyConfiguration(InMemoryBus.CreateMessageTopology());
            var busConfiguration = new InMemoryBusConfiguration(topology, new Uri("loopback://localhost/"));
            busConfiguration.HostConfiguration.LogContext = new BusLogContext(NullLoggerFactory.Instance);
            var configuration = new InMemoryReceiveEndpointConfiguration(busConfiguration.HostConfiguration, endpointName,
                busConfiguration.CreateEndpointConfiguration(isBusEndpoint));

            Context = new TransportInMemoryReceiveEndpointContext(busConfiguration.HostConfiguration, configuration);
            Endpoint = new ReceiveEndpoint(this, Context);
            _stopCompletion = stopCompletion ?? Task.CompletedTask;
        }

        public ReceiveEndpointContext Context { get; }
        public ReceiveEndpoint Endpoint { get; }
        public int StopCount { get; private set; }
        public TaskCompletionSource<bool> StopStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public long DeliveryCount => 0;
        public int ConcurrentDeliveryCount => 0;

        public ReceiveTransportHandle Start()
        {
            return this;
        }

        public Task Stop(CancellationToken cancellationToken = default)
        {
            StopCount++;
            StopStarted.TrySetResult(true);

            return _stopCompletion;
        }

        public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
        {
            return Context.ConnectReceiveObserver(observer);
        }

        public ConnectHandle ConnectReceiveTransportObserver(IReceiveTransportObserver observer)
        {
            return Context.ConnectReceiveTransportObserver(observer);
        }

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
        {
            return Context.ConnectPublishObserver(observer);
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer)
        {
            return Context.ConnectSendObserver(observer);
        }

        public void Probe(ProbeContext context)
        {
        }
    }
}
