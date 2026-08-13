using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Helpers;
using Source.Models;

namespace Source.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly AppDbContext _context;

        public FavoritesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var items = await _context.FavoriteItems
                .Include(f => f.FastFood)
                .Include(f => f.Combo)
                .Where(f => f.UserId == userId.Value)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return View(items);
        }
    }
}
