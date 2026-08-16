using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Models;

namespace Source.Controllers
{
    public class ShopController : Controller
    {
        private readonly AppDbContext _context;

        public ShopController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Shop/{id} — trang cửa hàng của người bán (thật, không fake)
        [Route("Shop/{id:int}")]
        [Route("Shop/Index/{id:int}")]
        public async Task<IActionResult> Index(int id)
        {
            var seller = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (seller == null)
            {
                return NotFound();
            }

            var foods = await _context.FastFoods
                .AsNoTracking()
                .Include(f => f.Category)
                .Where(f => f.SellerId == seller.Id && f.IsAvailable)
                .OrderByDescending(f => f.SoldCount)
                .ToListAsync();

            // Điểm đánh giá thật từ reviews của món thuộc shop
            var reviews = await _context.Reviews
                .AsNoTracking()
                .Where(r => r.FastFood != null && r.FastFood.SellerId == seller.Id && r.IsApproved)
                .ToListAsync();

            double avgRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0;
            var totalDelivered = await _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered && o.OrderDetails.Any(d => d.FastFood != null && d.FastFood.SellerId == seller.Id))
                .CountAsync();

            ViewBag.AvgRating = avgRating;
            ViewBag.ReviewCount = reviews.Count;
            ViewBag.TotalDelivered = totalDelivered;
            ViewBag.Seller = seller;
            return View(foods);
        }
    }
}