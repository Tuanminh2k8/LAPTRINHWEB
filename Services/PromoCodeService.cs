using Microsoft.EntityFrameworkCore;
using Source.Models;

namespace Source.Services
{
    public record PromoValidationResult(bool Success, string Message, decimal DiscountAmount, PromoCode? Promo);

    public interface IPromoCodeService
    {
        Task<PromoValidationResult> ValidateAsync(string? code, decimal subtotal);
    }

    public class PromoCodeService : IPromoCodeService
    {
        private readonly AppDbContext _context;

        public PromoCodeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PromoValidationResult> ValidateAsync(string? code, decimal subtotal)
        {
            if (string.IsNullOrWhiteSpace(code))
                return new PromoValidationResult(false, "Vui lòng nhập mã giảm giá.", 0, null);

            var normalized = code.Trim().ToUpper();
            var promo = await _context.PromoCodes
                .FirstOrDefaultAsync(p => p.Code.ToUpper() == normalized);

            if (promo == null)
                return new PromoValidationResult(false, "Mã giảm giá không tồn tại.", 0, null);

            if (!promo.IsActive)
                return new PromoValidationResult(false, "Mã giảm giá đã bị vô hiệu hóa.", 0, null);

            var now = DateTime.Now;
            if (promo.StartDate > now)
                return new PromoValidationResult(false, "Mã giảm giá chưa đến thời gian áp dụng.", 0, null);

            if (promo.EndDate.HasValue && promo.EndDate.Value < now)
                return new PromoValidationResult(false, "Mã giảm giá đã hết hạn.", 0, null);

            if (promo.MaxUsage > 0 && promo.UsedCount >= promo.MaxUsage)
                return new PromoValidationResult(false, "Mã giảm giá đã hết lượt sử dụng.", 0, null);

            if (subtotal < promo.MinOrderAmount)
                return new PromoValidationResult(false,
                    $"Đơn hàng tối thiểu {promo.MinOrderAmount:N0} ₫ để dùng mã này.", 0, null);

            decimal discount = promo.DiscountType == "Percent"
                ? Math.Round(subtotal * promo.DiscountValue / 100m, 0)
                : promo.DiscountValue;

            if (promo.MaxDiscountAmount > 0 && discount > promo.MaxDiscountAmount)
                discount = promo.MaxDiscountAmount;

            if (discount > subtotal) discount = subtotal;

            return new PromoValidationResult(true,
                $"Áp dụng mã {promo.Code} thành công! Giảm {discount:N0} ₫.", discount, promo);
        }
    }
}
