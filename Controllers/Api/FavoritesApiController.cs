using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Helpers;
using Source.Models;

namespace Source.Controllers.Api
{
    [Route("api/favorites")]
    [ApiController]
    [Authorize]
    public class FavoritesApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FavoritesApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/favorites — danh sách món yêu thích của khách
        [HttpGet]
        public async Task<IActionResult> List()
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (!userId.HasValue) return Unauthorized(new { message = "Vui lòng đăng nhập." });

            var items = await _context.FavoriteItems
                .AsNoTracking()
                .Where(f => f.UserId == userId.Value)
                .Select(f => new
                {
                    f.Id,
                    f.FastFoodId,
                    f.ComboId,
                    name = f.FastFood != null ? f.FastFood.Name : (f.Combo != null ? f.Combo.Name : "Món"),
                    imageUrl = f.FastFood != null ? f.FastFood.ImageUrl : (f.Combo != null ? f.Combo.ImageUrl : "/images/default_food.jpg"),
                    price = f.FastFood != null ? f.FastFood.Price : (f.Combo != null ? f.Combo.Price : 0m),
                    isCombo = f.ComboId != null
                })
                .ToListAsync();

            return Ok(items);
        }

        // GET: api/favorites/ids — chỉ trả về các Id đã yêu thích (để UI đánh dấu tim)
        [HttpGet("ids")]
        public async Task<IActionResult> Ids()
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (!userId.HasValue) return Ok(new { foods = new int[0], combos = new int[0] });

            var foodIds = await _context.FavoriteItems
                .Where(f => f.UserId == userId.Value && f.FastFoodId != null)
                .Select(f => f.FastFoodId!.Value).ToListAsync();
            var comboIds = await _context.FavoriteItems
                .Where(f => f.UserId == userId.Value && f.ComboId != null)
                .Select(f => f.ComboId!.Value).ToListAsync();

            return Ok(new { foods = foodIds, combos = comboIds });
        }

        // POST: api/favorites/toggle — thêm/bỏ món yêu thích
        [HttpPost("toggle")]
        public async Task<IActionResult> Toggle([FromBody] ToggleRequest req)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (!userId.HasValue) return Unauthorized(new { message = "Vui lòng đăng nhập để lưu món yêu thích." });

            if (req.FastFoodId == null && req.ComboId == null)
                return BadRequest(new { message = "Thiếu thông tin món." });

            var existing = await _context.FavoriteItems
                .FirstOrDefaultAsync(f =>
                    f.UserId == userId.Value &&
                    (req.FastFoodId == null || f.FastFoodId == req.FastFoodId) &&
                    (req.ComboId == null || f.ComboId == req.ComboId));

            if (existing != null)
            {
                _context.FavoriteItems.Remove(existing);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, isFavorited = false, message = "Đã bỏ khỏi món yêu thích." });
            }

            _context.FavoriteItems.Add(new FavoriteItem
            {
                UserId = userId.Value,
                FastFoodId = req.FastFoodId,
                ComboId = req.ComboId,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return Ok(new { success = true, isFavorited = true, message = "Đã thêm vào món yêu thích." });
        }

        public class ToggleRequest
        {
            public int? FastFoodId { get; set; }
            public int? ComboId { get; set; }
        }
    }
}
