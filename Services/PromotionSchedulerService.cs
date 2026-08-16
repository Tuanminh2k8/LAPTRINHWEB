using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Source.Services;

namespace Source.Services
{
    /// <summary>
    /// Tự động chuyển trạng thái khuyến mãi:
    /// - Scheduled -> Active khi StartDate <= now
    /// - Active -> Expired khi EndDate <= now
    /// Chạy định kỳ và phục hồi chính xác sau khi server restart (quét lại toàn bộ).
    /// </summary>
    public class PromotionSchedulerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PromotionSchedulerService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

        public PromotionSchedulerService(IServiceScopeFactory scopeFactory, ILogger<PromotionSchedulerService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Chạy ngay khi startup để xử lý các mã đến hạn/quá hạn trong thời gian server tắt
            await ProcessOnceAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_interval, stoppingToken);
                    await ProcessOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi chạy PromotionSchedulerService.");
                }
            }
        }

        private async Task ProcessOnceAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IPromotionService>();
            await service.ProcessScheduledAsync();
        }
    }
}
