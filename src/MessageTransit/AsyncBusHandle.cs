namespace MessageTransit
{
    using System.Threading;
    using System.Threading.Tasks;
    using Internals;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;


    public class AsyncBusHandle :
        IAsyncBusHandle
    {
        readonly IBusDepot _depot;
        readonly ILogger<MessageTransitBus> _logger;
        readonly IOptions<MessageTransitHostOptions> _options;
        readonly Task _startTask;
        readonly CancellationTokenSource _tokenSource;
        bool _stopped;

        public AsyncBusHandle(IBusDepot depot, ILogger<MessageTransitBus> logger, IOptions<MessageTransitHostOptions> options)
        {
            _depot = depot;
            _logger = logger;

            _options = options;

            _tokenSource = new CancellationTokenSource();

            _logger.LogInformation("Starting MessageTransit");

            _startTask = Task.Run(() => depot.Start(_tokenSource.Token), _tokenSource.Token);
        }

        public async ValueTask DisposeAsync()
        {
            if (_stopped)
                return;

            if (_startTask.IsCompletedSuccessfully())
            {
                if (_options.Value.StopTimeout.HasValue)
                {
                    using var tokenSource = new CancellationTokenSource(_options.Value.StopTimeout.Value);

                    _logger.LogInformation("Stopping MessageTransit (disposed)");

                    await _depot.Stop(tokenSource.Token).ConfigureAwait(false);
                }
                else
                    await _depot.Stop(CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                _logger.LogInformation("Cancel MessageTransit Start (disposed)");

                _tokenSource.Cancel();
            }

            _stopped = true;
        }
    }
}
