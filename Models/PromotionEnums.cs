namespace Source.Models;

/// <summary>
/// Trạng thái của một chương trình khuyến mãi / mã giảm giá.
/// Business rules:
/// - Draft: đang soạn thảo, User không thấy.
/// - Scheduled: đã lên lịch, chưa tới StartDate, User không thấy (trừ khi early visible).
/// - Active: đang hoạt động, User thấy và có thể dùng.
/// - Paused: tạm dừng, User không thể dùng.
/// - Expired: quá EndDate, User không thể dùng.
/// - Disabled: bị vô hiệu hóa, không hiển thị.
/// </summary>
public enum PromotionStatus
{
    Draft,
    Scheduled,
    Active,
    Paused,
    Expired,
    Disabled
}

/// <summary>Người sở hữu mã khuyến mãi.</summary>
public enum PromotionOwnerRole
{
    Admin,
    Seller
}

/// <summary>Trạng thái một lượt sử dụng mã.</summary>
public enum PromotionUsageStatus
{
    Used,
    Cancelled
}

/// <summary>
/// Loại giảm giá. Giữ đồng bộ với giá trị string cũ ("Percent"/"Amount")
/// để tương thích ngược với dữ liệu seed / PromoCodeService hiện tại.
/// Có thể mở rộng: FREESHIP, BUY_X_GET_Y, PRODUCT_DISCOUNT, CATEGORY_DISCOUNT.
/// </summary>
public enum PromotionDiscountType
{
    Percent,
    Amount
}
