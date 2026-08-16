using Source.Models;

namespace Source.Services
{
    public record PromotionValidationResult(
        bool Success,
        string Message,
        decimal DiscountAmount,
        PromoCode? Promo,
        PromotionStatus? EvaluatedStatus);

    public interface IPromotionService
    {
        Task<PromotionValidationResult> ValidateAsync(string? code, decimal subtotal, int? userId = null);
        Task<PromotionUsage?> RedeemAsync(int promotionId, int? userId, int? orderId, decimal originalAmount, decimal discount, string? ipAddress = null);
        Task CancelUsageAsync(int usageId, string? reason = null);

        // Admin / Seller management
        Task<PromoCode> CreateAsync(PromoCode model, string ownerRole, int? sellerId, string? createdBy);
        Task<PromoCode?> UpdateAsync(int id, PromoCode model, int? currentUserId, string currentRole, string? updatedBy);
        Task PublishAsync(int id, int? currentUserId, string currentRole);
        Task ScheduleAsync(int id, DateTime start, DateTime? end, int? currentUserId, string currentRole);
        Task PauseAsync(int id, int? currentUserId, string currentRole);
        Task ActivateAsync(int id, int? currentUserId, string currentRole);
        Task ExpireAsync(int id, int? currentUserId, string currentRole);
        Task EarlyPublishAsync(int id, bool usableEarly, int? currentUserId, string currentRole);
        Task SoftDeleteAsync(int id, int? currentUserId, string currentRole);

        // Queries
        Task<List<PromoCode>> GetPublicPromotionsAsync();
        Task<List<PromoCode>> GetAllAsync(string? role = null, int? sellerId = null);
        Task<PromoCode?> GetByIdAsync(int id);
        Task<object> GetStatisticsAsync(int? promotionId = null, int? sellerId = null);
        Task<List<PromotionUsage>> GetUsageHistoryAsync(int promotionId, int page = 1, int pageSize = 50);

        // Scheduler
        Task ProcessScheduledAsync();
    }
}
