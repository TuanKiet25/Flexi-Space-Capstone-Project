using FlexiSpace.Application.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlexiSpace.Web.BackgroundServices
{
    public class ContractExpirationWorker : PeriodicBackgroundService
    {
        public ContractExpirationWorker(IServiceScopeFactory scopeFactory, ILogger<ContractExpirationWorker> logger)
            : base(
                scopeFactory,
                logger,
                TimeSpan.FromDays(1), // Run once every 24 hours
                CalculateInitialDelay()) // Delay until next midnight
        {
        }

        private static TimeSpan CalculateInitialDelay()
        {
            var now = DateTime.Now;
            var nextMidnight = now.Date.AddDays(1); // 00:00 AM of the next day
            var delay = nextMidnight - now;
            return delay;
        }

        protected override async Task ExecuteWorkAsync(IServiceProvider serviceProvider, CancellationToken stoppingToken)
        {
            _logger.LogInformation("ContractExpirationWorker starting check at {Time}...", DateTime.Now);

            var contractService = serviceProvider.GetRequiredService<IContractService>();
            var result = await contractService.DeactivateExpiredContractsAsync();

            if (result.IsSuccess)
            {
                _logger.LogInformation("ContractExpirationWorker execution completed: {Message} (Expired count: {Count})", result.Message, result.Data);
            }
            else
            {
                _logger.LogError("ContractExpirationWorker execution failed: {Message}", result.Message);
            }
        }
    }
}
