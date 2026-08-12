using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlexiSpace.Web.BackgroundServices
{
    public abstract class PeriodicBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        protected readonly ILogger _logger;
        private readonly TimeSpan _period;
        private readonly TimeSpan? _initialDelay;

        protected PeriodicBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger logger,
            TimeSpan period,
            TimeSpan? initialDelay = null)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _period = period;
            _initialDelay = initialDelay;
        }

        protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{ServiceName} is starting.", GetType().Name);

            if (_initialDelay.HasValue && _initialDelay.Value > TimeSpan.Zero)
            {
                _logger.LogInformation("{ServiceName} is waiting for initial delay of {Delay} before first execution.", GetType().Name, _initialDelay.Value);
                try
                {
                    await Task.Delay(_initialDelay.Value, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("{ServiceName} execution canceled during initial delay.", GetType().Name);
                    return;
                }
            }

            // Run the first execution immediately after initial delay
            if (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ExecuteInScopeAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing task in {ServiceName} during initial run.", GetType().Name);
                }
            }

            using var timer = new PeriodicTimer(_period);
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ExecuteInScopeAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing task in {ServiceName}", GetType().Name);
                }
            }

            _logger.LogInformation("{ServiceName} is stopping.", GetType().Name);
        }

        private async Task ExecuteInScopeAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            await ExecuteWorkAsync(scope.ServiceProvider, stoppingToken);
        }

        protected abstract Task ExecuteWorkAsync(IServiceProvider serviceProvider, CancellationToken stoppingToken);
    }
}
