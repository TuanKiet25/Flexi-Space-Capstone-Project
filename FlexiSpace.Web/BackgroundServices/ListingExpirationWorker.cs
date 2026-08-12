using FlexiSpace.Application.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlexiSpace.Web.BackgroundServices
{
    public class ListingExpirationWorker : PeriodicBackgroundService
    {
        public ListingExpirationWorker(IServiceScopeFactory scopeFactory, ILogger<ListingExpirationWorker> logger)
            : base(
                scopeFactory,
                logger,
                TimeSpan.FromDays(1), // Run once every 24 hours
                CalculateInitialDelay() // Delay until next midnight
                //TimeSpan.FromMinutes(5),
                //TimeSpan.FromSeconds(10)
                  ) 
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
            _logger.LogInformation("ListingExpirationWorker starting check at {Time}...", DateTime.Now);

            var listingService = serviceProvider.GetRequiredService<IListingService>();
            var result = await listingService.DeactivateExpiredListingsAsync();

            if (result.IsSuccess)
            {
                _logger.LogInformation("ListingExpirationWorker execution completed: {Message} (Deactivated count: {Count})", result.Message, result.Data);
            }
            else
            {
                _logger.LogError("ListingExpirationWorker execution failed: {Message}", result.Message);
            }
        }
    }
}
