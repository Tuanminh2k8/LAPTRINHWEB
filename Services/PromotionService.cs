using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Source.Models;

namespace Source.Services
{
    public class PromotionService : IPromotionService
    {
        private readonly AppDbContext _context;

        public PromotionService(AppDbContext context)
        {
            _context = context;
        }

        #region Validation

        public async Task<PromotionValidationResult> ValidateAsync(string? code, decimal subtotal, int? userId = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Fail("Vui lòng nhập mã giảm giá.", null);

            var normalized = code.Trim().ToUpper();
            var promo = await _context.PromoCodes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == normalized);

            if (promo == null)
                return Fail("Mã giảm giá không tồn tại.", null);

            if (promo.IsDeleted)
                return Fail("Mã giảm giá không tồn tại.", null);

            var now = DateTime.Now;
            var status = (PromotionStatus)Enum.Parse(typeof(PromotionStatus), promo.Status);

            switch (status)
            {
                case PromotionStatus.Disabled:
                    return Fail("Mã giảm giá đã bị vô hiệu hóa.", status);
                case PromotionStatus.Expired:
                    return Fail("Mã giảm giá đã hết hạn.", status);
                case PromotionStatus.Paused:
                    return Fail("Mã giảm giá tạm ngừng sử dụng.", status);
                case PromotionStatus.Draft:
                    return Fail("Mã giảm giá chưa được công bố.", status);
                case PromotionStatus.Scheduled:
                    if (promo.IsEarlyPublished && promo.IsVisibleEarly && promo.IsUsableEarly)
                        break; // usable early
                    if (promo.IsEarlyPublished && promo.IsVisibleEarly)
                        return Fail("Mã đang được giới thiệu, chưa thể sử dụng lúc này.", status);
                    return Fail("Mã giảm giá chưa đến thời gian áp dụng.", status);
                case PromotionStatus.Active:
                    if (now < promo.StartDate && !(promo.IsEarlyPublished && promo.IsUsableEarly))
                        return Fail("Mã giảm giá chưa đến thời gian áp dụng.", status);
                    if (promo.EndDate.HasValue && now > promo.EndDate.Value)
                        return Fail("Mã giảm giá đã hết hạn.", status);
                    break;
            }

            if (promo.MaxUsage > 0 && promo.UsedCount >= promo.MaxUsage)
                return Fail("Mã giảm giá đã hết lượt sử dụng.", status);

            if (userId.HasValue && promo.MaxUsagePerUser > 0)
            {
                var usedByUser = await _context.PromotionUsages
                    .AsNoTracking()
                    .CountAsync(u => u.PromotionId == promo.Id && u.UserId == userId.Value && u.Status == nameof(PromotionUsageStatus.Used));
                if (usedByUser >= promo.MaxUsagePerUser)
                    return Fail("Bạn đã sử dụng mã này đủ số lần cho phép.", status);
            }

            if (subtotal < promo.MinOrderAmount)
                return Fail($"Đơn hàng tối thiểu {promo.MinOrderAmount:N0} ₫ để dùng mã này.", status);

            decimal discount = promo.DiscountType == nameof(PromotionDiscountType.Percent)
                ? Math.Round(subtotal * promo.DiscountValue / 100m, 0)
                : promo.DiscountValue;

            if (promo.MaxDiscountAmount > 0 && discount > promo.MaxDiscountAmount)
                discount = promo.MaxDiscountAmount;

            if (discount > subtotal) discount = subtotal;

            return new PromotionValidationResult(true,
                $"Áp dụng mã {promo.Code} thành công! Giảm {discount:N0} ₫.", discount, promo, status);
        }

        private static PromotionValidationResult Fail(string msg, PromotionStatus? status) =>
            new(false, msg, 0, null, status);

        #endregion

        #region Redeem (concurrency-safe)

        public async Task<PromotionUsage?> RedeemAsync(int promotionId, int? userId, int? orderId,
            decimal originalAmount, decimal discount, string? ipAddress = null)
        {
            var now = DateTime.Now;

            // Chỉ mở transaction nếu chưa có transaction nào đang hoạt động
            var hasOuter = _context.Database.CurrentTransaction != null;
            var tx = hasOuter ? null : await _context.Database.BeginTransactionAsync();

            try
            {
                // Reload để kiểm tra trạng thái thực tế
                var promo = await _context.PromoCodes
                    .FirstOrDefaultAsync(p => p.Id == promotionId);

                if (promo == null || promo.IsDeleted)
                    return null;

                var status = (PromotionStatus)Enum.Parse(typeof(PromotionStatus), promo.Status);
                bool isEarlyUsable = promo.IsEarlyPublished && promo.IsUsableEarly;
                bool usableNow = (status == PromotionStatus.Active || (status == PromotionStatus.Scheduled && isEarlyUsable))
                                 && (now >= promo.StartDate || isEarlyUsable)
                                 && (!promo.EndDate.HasValue || now <= promo.EndDate.Value);
                if (!usableNow)
                    return null;

                // Atomic increment, chỉ thành công khi còn lượt và đúng trạng thái
                // (cho phép cả mã Scheduled được đẩy lên trước & cho dùng sớm)
                var rows = await _context.PromoCodes
                    .Where(p => p.Id == promotionId
                                && !p.IsDeleted
                                && (p.Status == nameof(PromotionStatus.Active) ||
                                    (p.Status == nameof(PromotionStatus.Scheduled) && p.IsEarlyPublished && p.IsUsableEarly))
                                && (p.MaxUsage == 0 || p.UsedCount < p.MaxUsage))
                    .ExecuteUpdateAsync(p => p.SetProperty(x => x.UsedCount, x => x.UsedCount + 1));

                if (rows == 0)
                    return null; // hết lượt hoặc không hợp lệ (concurrency safe)

                var usage = new PromotionUsage
                {
                    PromotionId = promotionId,
                    UserId = userId,
                    OrderId = orderId,
                    DiscountAmount = discount,
                    OriginalOrderAmount = originalAmount,
                    FinalOrderAmount = originalAmount - discount,
                    UsedAt = now,
                    Status = nameof(PromotionUsageStatus.Used),
                    IpAddress = ipAddress
                };

                _context.PromotionUsages.Add(usage);
                await _context.SaveChangesAsync();
                if (tx != null) await tx.CommitAsync();

                return usage;
            }
            catch
            {
                if (tx != null) await tx.RollbackAsync();
                throw;
            }
        }

        public async Task CancelUsageAsync(int usageId, string? reason = null)
        {
            var usage = await _context.PromotionUsages.FirstOrDefaultAsync(u => u.Id == usageId);
            if (usage == null || usage.Status == nameof(PromotionUsageStatus.Cancelled))
                return;

            var hasOuter = _context.Database.CurrentTransaction != null;
            var tx = hasOuter ? null : await _context.Database.BeginTransactionAsync();

            try
            {
                // Hoàn lại UsedCount (chỉ nếu đang tính là đã dùng)
                await _context.PromoCodes
                    .Where(p => p.Id == usage.PromotionId && p.UsedCount > 0)
                    .ExecuteUpdateAsync(p => p.SetProperty(x => x.UsedCount, x => x.UsedCount - 1));

                usage.Status = nameof(PromotionUsageStatus.Cancelled);
                usage.CancellationReason = reason;
                await _context.SaveChangesAsync();
                if (tx != null) await tx.CommitAsync();
            }
            catch
            {
                if (tx != null) await tx.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region Authorization

        private void EnsureCanManage(PromoCode promo, int? currentUserId, string currentRole)
        {
            if (currentRole == nameof(PromotionOwnerRole.Admin))
                return;

            if (currentRole == nameof(PromotionOwnerRole.Seller))
            {
                if (promo.OwnerRole == nameof(PromotionOwnerRole.Seller) && promo.SellerId == currentUserId)
                    return;
                throw new UnauthorizedAccessException("Bạn chỉ được quản lý mã giảm giá của chính mình.");
            }

            throw new UnauthorizedAccessException("Không có quyền quản lý mã giảm giá.");
        }

        #endregion

        #region Management

        public async Task<PromoCode> CreateAsync(PromoCode model, string ownerRole, int? sellerId, string? createdBy)
        {
            var promo = new PromoCode
            {
                Code = model.Code.Trim().ToUpper(),
                Name = model.Name,
                Description = model.Description,
                DiscountType = model.DiscountType,
                DiscountValue = model.DiscountValue,
                MinOrderAmount = model.MinOrderAmount,
                MaxDiscountAmount = model.MaxDiscountAmount,
                MaxUsage = model.MaxUsage,
                MaxUsagePerUser = model.MaxUsagePerUser,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                IsActive = true,
                Status = nameof(PromotionStatus.Draft),
                IsPublished = false,
                OwnerRole = ownerRole,
                SellerId = ownerRole == nameof(PromotionOwnerRole.Seller) ? sellerId : null,
                Priority = model.Priority,
                ImageUrl = model.ImageUrl,
                BannerUrl = model.BannerUrl,
                CreatedAt = DateTime.Now,
                CreatedBy = createdBy
            };

            _context.PromoCodes.Add(promo);
            await _context.SaveChangesAsync();
            return promo;
        }

        public async Task<PromoCode?> UpdateAsync(int id, PromoCode model, int? currentUserId, string currentRole, string? updatedBy)
        {
            var promo = await _context.PromoCodes.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (promo == null) return null;

            EnsureCanManage(promo, currentUserId, currentRole);

            promo.Code = model.Code.Trim().ToUpper();
            promo.Name = model.Name;
            promo.Description = model.Description;
            promo.DiscountType = model.DiscountType;
            promo.DiscountValue = model.DiscountValue;
            promo.MinOrderAmount = model.MinOrderAmount;
            promo.MaxDiscountAmount = model.MaxDiscountAmount;
            promo.MaxUsage = model.MaxUsage;
            promo.MaxUsagePerUser = model.MaxUsagePerUser;
            promo.StartDate = model.StartDate;
            promo.EndDate = model.EndDate;
            promo.Priority = model.Priority;
            promo.ImageUrl = model.ImageUrl;
            promo.BannerUrl = model.BannerUrl;

            // Các flag hiển thị / phát hành (được chỉnh từ form Edit)
            promo.IsPublished = model.IsPublished;
            promo.IsFeatured = model.IsFeatured;
            promo.IsEarlyPublished = model.IsEarlyPublished;
            promo.IsVisibleEarly = model.IsVisibleEarly;

            promo.UpdatedAt = DateTime.Now;
            promo.UpdatedBy = updatedBy;

            // Nếu đang ở Draft/Scheduled/Active mà ngày thay đổi, thẩm định lại trạng thái
            if (promo.Status is nameof(PromotionStatus.Draft) or nameof(PromotionStatus.Scheduled) or nameof(PromotionStatus.Active))
            {
                promo.Status = EvaluateStatus(promo, DateTime.Now).ToString();
            }

            await _context.SaveChangesAsync();
            return promo;
        }

        public async Task PublishAsync(int id, int? currentUserId, string currentRole)
        {
            var promo = await _context.PromoCodes.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (promo == null) return;
            EnsureCanManage(promo, currentUserId, currentRole);

            promo.IsPublished = true;
            promo.PublishedAt = DateTime.Now;
            promo.IsActive = true;
            var evaluated = EvaluateStatus(promo, DateTime.Now);
            // Nếu chưa tới giờ bắt đầu (Scheduled) thì đẩy lên trước để hiển thị sớm
            if (evaluated == PromotionStatus.Scheduled)
            {
                promo.IsEarlyPublished = true;
                promo.IsVisibleEarly = true;
            }
            promo.Status = evaluated.ToString();
            promo.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        public async Task ScheduleAsync(int id, DateTime start, DateTime? end, int? currentUserId, string currentRole)
        {
            var promo = await _context.PromoCodes.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (promo == null) return;
            EnsureCanManage(promo, currentUserId, currentRole);

            promo.StartDate = start;
            promo.EndDate = end;
            promo.Status = nameof(PromotionStatus.Scheduled);
            promo.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        public async Task PauseAsync(int id, int? currentUserId, string currentRole)
        {
            var promo = await _context.PromoCodes.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (promo == null) return;
            EnsureCanManage(promo, currentUserId, currentRole);

            promo.Status = nameof(PromotionStatus.Paused);
            promo.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        public async Task ActivateAsync(int id, int? currentUserId, string currentRole)
        {
            var promo = await _context.PromoCodes.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (promo == null) return;
            EnsureCanManage(promo, currentUserId, currentRole);

            promo.Status = EvaluateStatus(promo, DateTime.Now).ToString();
            promo.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        public async Task ExpireAsync(int id, int? currentUserId, string currentRole)
        {
            var promo = await _context.PromoCodes.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (promo == null) return;
            EnsureCanManage(promo, currentUserId, currentRole);

            promo.Status = nameof(PromotionStatus.Expired);
            promo.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        public async Task EarlyPublishAsync(int id, bool usableEarly, int? currentUserId, string currentRole)
        {
            var promo = await _context.PromoCodes.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (promo == null) return;
            EnsureCanManage(promo, currentUserId, currentRole);

            promo.IsEarlyPublished = true;
            promo.IsVisibleEarly = true;
            promo.IsUsableEarly = usableEarly;
            promo.IsPublished = true;
            promo.IsActive = true;
            promo.PublishedAt = DateTime.Now;
            promo.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        public async Task SoftDeleteAsync(int id, int? currentUserId, string currentRole)
        {
            var promo = await _context.PromoCodes.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (promo == null) return;
            EnsureCanManage(promo, currentUserId, currentRole);

            // Không xóa vật lý để giữ lịch sử usage
            promo.IsDeleted = true;
            promo.IsPublished = false;
            promo.Status = nameof(PromotionStatus.Disabled);
            promo.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Queries

        public async Task<List<PromoCode>> GetPublicPromotionsAsync()
        {
            return await _context.PromoCodes
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.IsPublished &&
                            (p.Status == nameof(PromotionStatus.Active) ||
                             (p.IsEarlyPublished && p.IsVisibleEarly)))
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.Priority)
                .ThenByDescending(p => p.StartDate)
                .ToListAsync();
        }

        public async Task<List<PromoCode>> GetAllAsync(string? role = null, int? sellerId = null)
        {
            var query = _context.PromoCodes.AsNoTracking().Where(p => !p.IsDeleted);

            if (role == nameof(PromotionOwnerRole.Seller) && sellerId.HasValue)
                query = query.Where(p => p.OwnerRole == nameof(PromotionOwnerRole.Seller) && p.SellerId == sellerId.Value);
            else if (role == nameof(PromotionOwnerRole.Admin))
                query = query.Where(p => p.OwnerRole == nameof(PromotionOwnerRole.Admin));

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<PromoCode?> GetByIdAsync(int id) =>
            await _context.PromoCodes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        public async Task<object> GetStatisticsAsync(int? promotionId = null, int? sellerId = null)
        {
            var usageQuery = _context.PromotionUsages.AsNoTracking().Where(u => u.Status == nameof(PromotionUsageStatus.Used));
            var promoQuery = _context.PromoCodes.AsNoTracking().Where(p => !p.IsDeleted);

            if (sellerId.HasValue)
                promoQuery = promoQuery.Where(p => p.OwnerRole == nameof(PromotionOwnerRole.Seller) && p.SellerId == sellerId.Value);

            if (promotionId.HasValue)
            {
                usageQuery = usageQuery.Where(u => u.PromotionId == promotionId.Value);
                promoQuery = promoQuery.Where(p => p.Id == promotionId.Value);
            }

            var totalUsed = await usageQuery.CountAsync();
            var totalDiscount = await usageQuery.SumAsync(u => (decimal?)u.DiscountAmount) ?? 0;
            var totalRevenue = await usageQuery.SumAsync(u => (decimal?)u.FinalOrderAmount) ?? 0;
            var totalOrders = await usageQuery.Select(u => u.OrderId).Distinct().CountAsync();

            var perPromotion = await promoQuery
                .Select(p => new
                {
                    p.Id,
                    p.Code,
                    p.Name,
                    p.OwnerRole,
                    p.Status,
                    p.MaxUsage,
                    p.UsedCount,
                    Remaining = p.MaxUsage == 0 ? int.MaxValue : Math.Max(0, p.MaxUsage - p.UsedCount),
                    Usages = _context.PromotionUsages.Count(u => u.PromotionId == p.Id && u.Status == nameof(PromotionUsageStatus.Used)),
                    DiscountGiven = _context.PromotionUsages.Where(u => u.PromotionId == p.Id && u.Status == nameof(PromotionUsageStatus.Used)).Sum(u => (decimal?)u.DiscountAmount) ?? 0
                })
                .ToListAsync();

            return new
            {
                TotalUsed = totalUsed,
                TotalDiscount = totalDiscount,
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                PerPromotion = perPromotion
            };
        }

        public async Task<List<PromotionUsage>> GetUsageHistoryAsync(int promotionId, int page = 1, int pageSize = 50)
        {
            page = Math.Max(1, page);
            return await _context.PromotionUsages
                .AsNoTracking()
                .Include(u => u.User)
                .Include(u => u.Order)
                .Where(u => u.PromotionId == promotionId)
                .OrderByDescending(u => u.UsedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        #endregion

        #region Scheduler

        public async Task ProcessScheduledAsync()
        {
            var now = DateTime.Now;
            var candidates = await _context.PromoCodes
                .Where(p => !p.IsDeleted &&
                            p.Status != nameof(PromotionStatus.Draft) &&
                            p.Status != nameof(PromotionStatus.Disabled) &&
                            p.Status != nameof(PromotionStatus.Paused))
                .ToListAsync();

            foreach (var p in candidates)
            {
                var evaluated = EvaluateStatus(p, now);
                if (evaluated.ToString() != p.Status)
                {
                    p.Status = evaluated.ToString();
                    p.UpdatedAt = now;
                }
            }

            await _context.SaveChangesAsync();
        }

        private static PromotionStatus EvaluateStatus(PromoCode p, DateTime now)
        {
            if (p.Status == nameof(PromotionStatus.Disabled)) return PromotionStatus.Disabled;
            if (p.Status == nameof(PromotionStatus.Paused)) return PromotionStatus.Paused;
            if (p.Status == nameof(PromotionStatus.Draft)) return PromotionStatus.Draft;
            if (p.EndDate.HasValue && now > p.EndDate.Value) return PromotionStatus.Expired;
            if (now < p.StartDate) return PromotionStatus.Scheduled;
            return PromotionStatus.Active;
        }

        #endregion
    }
}
