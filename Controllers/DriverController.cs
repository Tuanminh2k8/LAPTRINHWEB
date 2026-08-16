using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Helpers;
using Source.Models;

namespace Source.Controllers
{
    [Authorize]
    public class DriverController : Controller
    {
        private readonly AppDbContext _context;

        public DriverController(AppDbContext context)
        {
            _context = context;
        }

        // Kiểm tra user hiện tại có phải tài xế không
        private async Task<Driver?> CurrentDriver()
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (!userId.HasValue) return null;
            return await _context.Drivers.AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userId.Value && d.IsActive);
        }

        // GET: /Driver — bảng điều khiển tài xế (yêu cầu là tài xế)
        public async Task<IActionResult> Index()
        {
            var driver = await CurrentDriver();
            if (driver == null)
            {
                return RedirectToAction("NotDriver");
            }

            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.DriverId == driver.Id && !o.IsDeleted)
                .OrderByDescending(o => o.OrderDate)
                .Take(20)
                .ToListAsync();

            ViewBag.Driver = driver;
            ViewBag.InDeliveryCount = orders.Count(o => OrderStatus.InDelivery.Contains(o.Status));
            ViewBag.TotalDelivered = driver.TotalDeliveries;
            return View(orders);
        }

        // GET: /Driver/Order/5 — chi tiết đơn cho tài xế
        public async Task<IActionResult> Order(int id)
        {
            var driver = await CurrentDriver();
            if (driver == null) return RedirectToAction("NotDriver");

            var order = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(d => d.Modifiers)
                .Include(o => o.OrderDetails).ThenInclude(d => d.FastFood)
                .Include(o => o.OrderDetails).ThenInclude(d => d.Combo)
                .FirstOrDefaultAsync(o => o.Id == id && o.DriverId == driver.Id && !o.IsDeleted);

            if (order == null) return NotFound();

            ViewBag.Driver = driver;
            return View(order);
        }

        // GET: /Driver/NotDriver — thông báo tài khoản không phải tài xế
        public IActionResult NotDriver()
        {
            return View();
        }
    }
}