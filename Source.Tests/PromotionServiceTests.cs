using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Source.Models;
using Source.Services;
using Xunit;

namespace Source.Tests
{
    public class PromotionServiceTests
    {
        // Dùng SQL Server thật (local) để kiểm thử ExecuteUpdateAsync / transaction / concurrency.
        // DB test riêng biệt, không ảnh hưởng DB production của project.
        private const string TestConn =
            @"Server=localhost\SQLEXPRESS;Database=PolyFoodUnitTest;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;";

        private static AppDbContext NewContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(TestConn)
                .Options;
            var ctx = new AppDbContext(options);
            ctx.Database.EnsureCreated();
            return ctx;
        }

        private static async Task ClearAsync(AppDbContext ctx)
        {
            ctx.PromotionUsages.RemoveRange(ctx.PromotionUsages);
            ctx.PromoCodes.RemoveRange(ctx.PromoCodes);
            await ctx.SaveChangesAsync();
        }

        private static async Task<int> AddUserAsync(AppDbContext ctx)
        {
            var user = new User
            {
                Username = "u" + Guid.NewGuid().ToString("N")[..8],
                PasswordHash = "x",
                FullName = "Test",
                Email = Guid.NewGuid().ToString("N")[..8] + "@t.vn",
                PhoneNumber = "0901234567",
                Address = "addr",
                Role = "Customer"
            };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
            return user.Id;
        }

        private static PromoCode MakePromo(string code, decimal value, string discountType = "Percent",
            decimal min = 0, decimal maxDiscount = 0, int maxUsage = 0, int maxPerUser = 0,
            DateTime? start = null, DateTime? end = null, string status = "Active", bool published = true)
        {
            var now = DateTime.Now;
            return new PromoCode
            {
                Code = code,
                Name = code,
                DiscountType = discountType,
                DiscountValue = value,
                MinOrderAmount = min,
                MaxDiscountAmount = maxDiscount,
                MaxUsage = maxUsage,
                MaxUsagePerUser = maxPerUser,
                StartDate = start ?? now.AddDays(-1),
                EndDate = end ?? now.AddYears(1),
                Status = status,
                IsPublished = published,
                IsActive = true
            };
        }

        // TEST 1: Coupon bình thường
        [Fact]
        public async Task TEST1_NormalCoupon_Success()
        {
            using var ctx = NewContext();
            await ClearAsync(ctx);
            ctx.PromoCodes.Add(MakePromo("NORMAL20", 20, min: 50000));
            await ctx.SaveChangesAsync();
            var svc = new PromotionService(ctx);
            var r = await svc.ValidateAsync("normal20", 100000, 1);
            Assert.True(r.Success);
            Assert.Equal(20000m, r.DiscountAmount);
        }

        // TEST 2: Coupon hết hạn
        [Fact]
        public async Task TEST2_Expired_Fail()
        {
            using var ctx = NewContext();
            await ClearAsync(ctx);
            ctx.PromoCodes.Add(MakePromo("EXP", 10, end: DateTime.Now.AddDays(-1), status: "Expired"));
            await ctx.SaveChangesAsync();
            var svc = new PromotionService(ctx);
            var r = await svc.ValidateAsync("EXP", 100000, 1);
            Assert.False(r.Success);
        }

        // TEST 3: Coupon chưa bắt đầu
        [Fact]
        public async Task TEST3_NotStarted_Fail()
        {
            using var ctx = NewContext();
            await ClearAsync(ctx);
            ctx.PromoCodes.Add(MakePromo("FUTURE", 10, start: DateTime.Now.AddDays(5), status: "Scheduled"));
            await ctx.SaveChangesAsync();
            var svc = new PromotionService(ctx);
            var r = await svc.ValidateAsync("FUTURE", 100000, 1);
            Assert.False(r.Success);
        }

        // TEST 4: Early publish nhưng chưa usable
        [Fact]
        public async Task TEST4_EarlyPublishedNotUsable_Fail()
        {
            using var ctx = NewContext();
            await ClearAsync(ctx);
            var p = MakePromo("EARLY", 10, start: DateTime.Now.AddDays(5), status: "Scheduled");
            p.IsEarlyPublished = true; p.IsVisibleEarly = true; p.IsUsableEarly = false;
            ctx.PromoCodes.Add(p);
            await ctx.SaveChangesAsync();
            var svc = new PromotionService(ctx);
            var r = await svc.ValidateAsync("EARLY", 100000, 1);
            Assert.False(r.Success);
        }

        // TEST 5 + TEST 9: Max usage = 2, dùng đến cạn, slot cuối concurrency-safe.
        // Mỗi request dùng 1 DbContext riêng (như scoped per-request trong production);
        // độ an toàn đến từ atomic increment ở tầng DB.
        [Fact]
        public async Task TEST5_And_TEST9_MaxUsage_RejectsWhenExhausted()
        {
            using (var ctx = NewContext())
            {
                await ClearAsync(ctx);
                var promo = MakePromo("MAX", 10, maxUsage: 2);
                ctx.PromoCodes.Add(promo);
                await ctx.SaveChangesAsync();
            }

            var id = NewContext().PromoCodes.AsNoTracking().First().Id;

            // 12 request đồng thời, mỗi request 1 context riêng
            var tasks = Enumerable.Range(0, 12).Select(_ =>
            {
                var c = NewContext();
                var s = new PromotionService(c);
                return s.RedeemAsync(id, null, null, 100000, 10000);
            });
            var results = await Task.WhenAll(tasks);

            var successCount = results.Count(r => r != null);
            Assert.Equal(2, successCount); // chỉ đúng 2 lượt thành công
            Assert.Equal(2, NewContext().PromoCodes.AsNoTracking().First().UsedCount);
        }

        // TEST 6: User dùng vượt giới hạn/user
        [Fact]
        public async Task TEST6_PerUserLimit_Fail()
        {
            using var ctx = NewContext();
            await ClearAsync(ctx);
            var promo = MakePromo("PERUSER", 10, maxPerUser: 1);
            ctx.PromoCodes.Add(promo);
            await ctx.SaveChangesAsync();
            var id = promo.Id;
            var svc = new PromotionService(ctx);
            var uid = await AddUserAsync(ctx);
            Assert.NotNull(await svc.RedeemAsync(id, uid, null, 100000, 10000));
            var r = await svc.ValidateAsync("PERUSER", 100000, uid);
            Assert.False(r.Success);
        }

        // TEST 7: Order không đạt minimum
        [Fact]
        public async Task TEST7_MinAmount_Fail()
        {
            using var ctx = NewContext();
            await ClearAsync(ctx);
            ctx.PromoCodes.Add(MakePromo("MIN", 10, min: 150000));
            await ctx.SaveChangesAsync();
            var svc = new PromotionService(ctx);
            var r = await svc.ValidateAsync("MIN", 100000, 1);
            Assert.False(r.Success);
        }

        // TEST 8: Discount vượt maximum discount
        [Fact]
        public async Task TEST8_MaxDiscountClamp()
        {
            using var ctx = NewContext();
            await ClearAsync(ctx);
            ctx.PromoCodes.Add(MakePromo("CAP", 50, "Percent", min: 0, maxDiscount: 30000));
            await ctx.SaveChangesAsync();
            var svc = new PromotionService(ctx);
            var r = await svc.ValidateAsync("CAP", 200000, 1);
            Assert.True(r.Success);
            Assert.Equal(30000m, r.DiscountAmount);
        }

        // TEST 10: Seller sửa coupon của seller khác -> FAIL
        [Fact]
        public async Task TEST10_SellerEditOther_Throws()
        {
            using var ctx = NewContext();
            await ClearAsync(ctx);
            var seller = await AddUserAsync(ctx);
            var other = await AddUserAsync(ctx);
            var p = MakePromo("S1", 10);
            p.OwnerRole = "Seller"; p.SellerId = seller;
            ctx.PromoCodes.Add(p);
            await ctx.SaveChangesAsync();
            var id = p.Id;
            var svc = new PromotionService(ctx);
            Assert.Throws<UnauthorizedAccessException>(() =>
                svc.UpdateAsync(id, MakePromo("S1", 20), other, "Seller", "seller" + other).GetAwaiter().GetResult());
        }

        // TEST 11: Seller sửa coupon của chính mình -> PASS
        [Fact]
        public async Task TEST11_SellerEditOwn_Pass()
        {
            using var ctx = NewContext();
            await ClearAsync(ctx);
            var seller = await AddUserAsync(ctx);
            var p = MakePromo("S2", 10);
            p.OwnerRole = "Seller"; p.SellerId = seller;
            ctx.PromoCodes.Add(p);
            await ctx.SaveChangesAsync();
            var id = p.Id;
            var svc = new PromotionService(ctx);
            var updated = await svc.UpdateAsync(id, MakePromo("S2", 25), seller, "Seller", "seller" + seller);
            Assert.NotNull(updated);
            Assert.Equal(25m, updated!.DiscountValue);
        }

        // TEST 12: Admin sửa coupon seller -> PASS
        [Fact]
        public async Task TEST12_AdminEditSeller_Pass()
        {
            using var ctx = NewContext();
            await ClearAsync(ctx);
            var seller = await AddUserAsync(ctx);
            var p = MakePromo("S3", 10);
            p.OwnerRole = "Seller"; p.SellerId = seller;
            ctx.PromoCodes.Add(p);
            await ctx.SaveChangesAsync();
            var id = p.Id;
            var svc = new PromotionService(ctx);
            var updated = await svc.UpdateAsync(id, MakePromo("S3", 99), null, "Admin", "admin");
            Assert.NotNull(updated);
            Assert.Equal(99m, updated!.DiscountValue);
        }

        // TEST 13: Admin early publish -> PASS (usable early)
        [Fact]
        public async Task TEST13_AdminEarlyPublish_Pass()
        {
            using var ctx = NewContext();
            await ClearAsync(ctx);
            var p = MakePromo("EP", 10, start: DateTime.Now.AddDays(5), status: "Scheduled");
            ctx.PromoCodes.Add(p);
            await ctx.SaveChangesAsync();
            var id = p.Id;
            var svc = new PromotionService(ctx);
            await svc.EarlyPublishAsync(id, true, null, "Admin");
            var r = await svc.ValidateAsync("EP", 100000, 1);
            Assert.True(r.Success);
        }

        // TEST 14: Scheduler auto ACTIVE khi StartAt tới
        [Fact]
        public async Task TEST14_SchedulerActivates()
        {
            using var ctx = NewContext();
            await ClearAsync(ctx);
            ctx.PromoCodes.Add(MakePromo("SCH", 10, start: DateTime.Now.AddMinutes(-1), status: "Scheduled"));
            await ctx.SaveChangesAsync();
            var svc = new PromotionService(ctx);
            await svc.ProcessScheduledAsync();
            Assert.Equal("Active", NewContext().PromoCodes.AsNoTracking().First().Status);
        }

        // TEST 15: Scheduler auto EXPIRED khi EndAt tới
        [Fact]
        public async Task TEST15_SchedulerExpires()
        {
            using var ctx = NewContext();
            await ClearAsync(ctx);
            ctx.PromoCodes.Add(MakePromo("SCHX", 10, end: DateTime.Now.AddMinutes(-1), status: "Active"));
            await ctx.SaveChangesAsync();
            var svc = new PromotionService(ctx);
            await svc.ProcessScheduledAsync();
            Assert.Equal("Expired", NewContext().PromoCodes.AsNoTracking().First().Status);
        }

        // TEST 16: Server restart -> ProcessScheduledAsync vẫn xử lý đúng (quét lại)
        [Fact]
        public async Task TEST16_RestartRescan()
        {
            using var ctx = NewContext();
            await ClearAsync(ctx);
            ctx.PromoCodes.Add(MakePromo("R1", 10, start: DateTime.Now.AddDays(-1), end: DateTime.Now.AddMinutes(-1), status: "Active"));
            ctx.PromoCodes.Add(MakePromo("R2", 10, start: DateTime.Now.AddMinutes(-1), status: "Scheduled"));
            await ctx.SaveChangesAsync();
            var svc = new PromotionService(ctx);
            await svc.ProcessScheduledAsync();
            Assert.Equal("Expired", NewContext().PromoCodes.AsNoTracking().First(x => x.Code == "R1").Status);
            Assert.Equal("Active", NewContext().PromoCodes.AsNoTracking().First(x => x.Code == "R2").Status);
        }
    }
}
