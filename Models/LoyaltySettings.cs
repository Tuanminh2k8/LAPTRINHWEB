namespace Source.Models
{
    /// <summary>
    /// Cấu hình chương trình tích điểm Membership (như McDonald's Rewards / KFC Rewards).
    /// Tập trung toàn bộ tỷ lệ tại đây để dễ điều chỉnh; Admin có thể ghi đè qua bảng cấu hình nếu mở rộng.
    /// </summary>
    public static class LoyaltySettings
    {
        /// <summary>Cứ bao nhiêu VNĐ chi tiêu thì được 1 điểm.</summary>
        public const decimal EarnPerVnd = 10000m;

        /// <summary>Số điểm tối thiểu được phép dùng để đổi giảm giá.</summary>
        public const int MinRedeemPoints = 100;

        /// <summary>Đổi bao nhiêu điểm thì tương ứng DiscountVnd giảm giá.</summary>
        public const int RedeemPoints = 100;

        /// <summary>Giá trị VNĐ được giảm khi đổi RedeemPoints điểm.</summary>
        public const decimal RedeemValueVnd = 10000m;

        /// <summary>Tối đa % giá trị đơn hàng (sau promo) được giảm bằng điểm.</summary>
        public const decimal MaxRedeemPercentOfOrder = 0.5m;
    }
}
