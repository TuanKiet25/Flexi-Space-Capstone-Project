using FlexiSpace.Application.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlexiSpace.Web.BackgroundServices
{
    public class BannerExpirationWorker : PeriodicBackgroundService
    {
        public BannerExpirationWorker(IServiceScopeFactory scopeFactory, ILogger<BannerExpirationWorker> logger)
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
            _logger.LogInformation("BannerExpirationWorker starting check at {Time}...", DateTime.Now);

            var bannerService = serviceProvider.GetRequiredService<IBannerService>();
            var result = await bannerService.DeleteExpiredBannersAsync();

            if (result.IsSuccess)
            {
                _logger.LogInformation("BannerExpirationWorker execution completed: {Message} (Deleted count: {Count})", result.Message, result.Data);
            }
            else
            {
                _logger.LogError("BannerExpirationWorker execution failed: {Message}", result.Message);
            }
        }
    }
}
