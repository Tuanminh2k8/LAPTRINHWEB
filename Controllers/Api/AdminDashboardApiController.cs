using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Models;

namespace Source.Controllers.Api
{
    [Route("api/admin/dashboard")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminDashboardApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminDashboardApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/dashboard — dữ liệu cho biểu đồ + polling 10s của admin
        [HttpGet]
        public async Task<ActionResult> GetDashboard()
        {
            var now = DateTime.Now;

            // Doanh thu theo ngày (7 ngày gần nhất) — chỉ tính đơn đã giao
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => now.Date.AddDays(-i))
                .ToList();

            var revenueByDay = await _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered && !o.IsDeleted && o.OrderDate >= now.Date.AddDays(-6))
                .ToListAsync();

            var revenueData = last7Days.Select(day => new
            {
                label = day.ToString("dd/MM"),
                revenue = revenueByDay.Where(o => o.OrderDate.Date == day).Sum(o => o.TotalAmount),
                orders = revenueByDay.Count(o => o.OrderDate.Date == day)
            }).OrderBy(x => DateTime.ParseExact(x.label, "dd/MM", System.Globalization.CultureInfo.InvariantCulture)).ToList();

            // Doanh thu theo món (top 8 bán chạy theo SoldCount)
            var topFoods = await _context.FastFoods
                .AsNoTracking()
                .OrderByDescending(f => f.SoldCount)
                .Take(8)
                .Select(f => new { f.Name, f.SoldCount })
                .ToListAsync();

            // Đơn theo trạng thái (donut)
            var statusCounts = new Dictionary<string, int>();
            foreach (var s in OrderStatus.All)
            {
                statusCounts[s] = await _context.Orders.CountAsync(o => o.Status == s && !o.IsDeleted);
            }

            return Ok(new
            {
                revenueByDay = revenueData,
                topFoods,
                statusCounts,
                statusLabels = OrderStatus.All.ToDictionary(s => s, OrderStatus.GetLabel),
                serverTime = now
            });
        }

        // GET: api/admin/dashboard/recent — đơn hàng mới nhất cho bảng realtime
        [HttpGet("recent")]
        public async Task<ActionResult> GetRecentOrders()
        {
            var recent = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Where(o => !o.IsDeleted)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new
                {
                    o.Id,
                    o.OrderDate,
                    o.OrderType,
                    o.Status,
                    statusLabel = OrderStatus.GetLabel(o.Status),
                    badgeClass = OrderStatus.GetBadgeClass(o.Status),
                    receiverName = o.ReceiverName,
                    receiverPhone = o.ReceiverPhone,
                    o.TotalAmount,
                    customerName = o.User != null ? o.User.FullName : "Khách vãng lai"
                })
                .ToListAsync();

            return Ok(recent);
        }
    }
}