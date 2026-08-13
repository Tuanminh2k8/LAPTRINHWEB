using Source.Models;
using Microsoft.EntityFrameworkCore;

namespace Source.Services
{
    public interface ILoyaltyService
    {
        int CalculateEarnedPoints(decimal amount);
        bool HasEarned(int orderId);
        void Award(Order order);
        (bool ok, decimal discount, int pointsUsed, string message) PreviewRedeem(int userId, int requested, decimal amountAfterPromo);
        void ApplyRedeem(int userId, int usablePoints, decimal discount);
    }

    public class LoyaltyService : ILoyaltyService
    {
        private readonly AppDbContext _context;

        public LoyaltyService(AppDbContext context)
        {
            _context = context;
        }

        public int CalculateEarnedPoints(decimal amount)
            => (int)Math.Floor(amount / LoyaltySettings.EarnPerVnd);

        public bool HasEarned(int orderId)
            => _context.PointTransactions.Any(t => t.OrderId == orderId && t.Type == "Earn");

        /// <summary>Tích điểm khi đơn hoàn thành. Không lưu (caller commit). Idempotent.</summary>
        public void Award(Order order)
        {
            if (!order.UserId.HasValue || HasEarned(order.Id)) return;
            var points = CalculateEarnedPoints(order.TotalAmount);
            if (points <= 0) return;

            var user = _context.Users.Find(order.UserId.Value);
            if (user == null) return;

            user.Points += points;
            user.TotalSpent += order.TotalAmount;

            _context.PointTransactions.Add(new PointTransaction
            {
                UserId = user.Id,
                OrderId = order.Id,
                Points = points,
                Type = "Earn",
                BalanceAfter = user.Points,
                Note = $"Tích {points} điểm từ đơn #{order.Id}",
                CreatedAt = DateTime.Now
            });
        }

        public (bool ok, decimal discount, int pointsUsed, string message) PreviewRedeem(int userId, int requested, decimal amountAfterPromo)
        {
            if (requested <= 0) return (false, 0, 0, "Không có điểm để đổi.");

            var user = _context.Users.Find(userId);
            if (user == null) return (false, 0, 0, "Không tìm thấy người dùng.");

            if (requested > user.Points) return (false, 0, 0, "Số điểm yêu cầu vượt quá điểm hiện có.");

            // Tròn xuống bội số RedeemPoints
            var usable = (requested / LoyaltySettings.RedeemPoints) * LoyaltySettings.RedeemPoints;
            if (usable < LoyaltySettings.MinRedeemPoints)
                return (false, 0, 0, $"Cần tối thiểu {LoyaltySettings.MinRedeemPoints} điểm mỗi lần đổi.");

            var discount = (usable / LoyaltySettings.RedeemPoints) * LoyaltySettings.RedeemValueVnd;
            var maxDiscount = amountAfterPromo * LoyaltySettings.MaxRedeemPercentOfOrder;
            if (discount > maxDiscount)
            {
                discount = Math.Floor(maxDiscount / LoyaltySettings.RedeemValueVnd) * LoyaltySettings.RedeemValueVnd;
                usable = (int)(discount / LoyaltySettings.RedeemValueVnd * LoyaltySettings.RedeemPoints);
            }

            return (true, discount, usable, "OK");
        }

        /// <summary>Trừ điểm khi khách đổi. Không lưu (caller commit).</summary>
        public void ApplyRedeem(int userId, int usablePoints, decimal discount)
        {
            var user = _context.Users.Find(userId);
            if (user == null) return;

            user.Points -= usablePoints;
            _context.PointTransactions.Add(new PointTransaction
            {
                UserId = user.Id,
                Points = -usablePoints,
                Type = "Redeem",
                BalanceAfter = user.Points,
                Note = $"Đổi {usablePoints} điểm giảm {discount:N0}₫",
                CreatedAt = DateTime.Now
            });
        }
    }
}
