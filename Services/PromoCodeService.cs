using Microsoft.EntityFrameworkCore;
using Source.Models;

namespace Source.Services
{
    public record PromoValidationResult(bool Success, string Message, decimal DiscountAmount, PromoCode? Promo);

    public interface IPromoCodeService
    {
        Task<PromoValidationResult> ValidateAsync(string? code, decimal subtotal);
    }

    /// <summary>
    /// Lớp tương thích ngược cho luồng checkout cũ (CartController / CartApiController / OrdersApiController).
    /// Ủy quyền sang IPromotionService để có một nguồn验证逻辑 duy nhất.
    /// Không userId nên bỏ qua kiểm tra giới hạn mỗi user.
    /// </summary>
    public class PromoCodeService : IPromoCodeService
    {
        private readonly AppDbContext _context;
        private readonly IPromotionService _promotionService;

        public PromoCodeService(AppDbContext context, IPromotionService promotionService)
        {
            _context = context;
            _promotionService = promotionService;
        }

        public async Task<PromoValidationResult> ValidateAsync(string? code, decimal subtotal)
        {
            var result = await _promotionService.ValidateAsync(code, subtotal, null);
            return new PromoValidationResult(result.Success, result.Message, result.DiscountAmount, result.Promo);
        }
    }
}
