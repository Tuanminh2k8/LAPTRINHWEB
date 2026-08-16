using Source.Models;

namespace Source.Services
{
    /// <summary>
    /// Chạy seed data trong nền sau khi app đã lắng nghe request.
    /// Tránh chặn startup (máy yếu / DB chậm → app vẫn phản hồi ngay).
    /// </summary>
    public class DatabaseSeederHostedService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DatabaseSeederHostedService> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public DatabaseSeederHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<DatabaseSeederHostedService> logger,
            IHostApplicationLifetime lifetime)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _lifetime = lifetime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Fire-and-forget: trả về ngay để server bắt đầu phục vụ request.
            _ = Task.Run(() => SeedAsync(_lifetime.ApplicationStopping), cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task SeedAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                _logger.LogInformation("[DatabaseSeeder] Bắt đầu seed data trong nền...");
                await DbInitializer.SeedAsync(context);
                _logger.LogInformation("[DatabaseSeeder] Hoàn tất seed data.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("[DatabaseSeeder] Bị hủy do app đang tắt.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DatabaseSeeder] Lỗi seed data trong nền (app vẫn chạy).");
            }
        }
    }
}
