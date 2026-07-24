using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Models;

namespace Source.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchName, int? categoryId)
        {
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = categories;

            var query = _context.FastFoods.Include(f => f.Category).AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(f => f.Name.Contains(searchName));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(f => f.CategoryId == categoryId.Value);
            }

            var foods = await query.ToListAsync();
            var combos = await _context.Combos.Include(c => c.ComboDetails).ThenInclude(cd => cd.FastFood).ToListAsync();

            ViewBag.Combos = combos;
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SearchName = searchName;

            return View(foods);
        }

        // Advanced Search (AJAX friendly)
        public async Task<IActionResult> AdvancedSearch(string? name, decimal? minPrice, decimal? maxPrice, int? categoryId, string? theme, string? description)
        {
            var query = _context.FastFoods.Include(f => f.Category).AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(f => f.Name.Contains(name));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(f => f.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(f => f.Price <= maxPrice.Value);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(f => f.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(theme))
            {
                query = query.Where(f => f.Theme.Contains(theme));
            }

            if (!string.IsNullOrEmpty(description))
            {
                query = query.Where(f => f.Description.Contains(description));
            }

            var results = await query.ToListAsync();

            // Check if AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_FoodListPartial", results);
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Combos = await _context.Combos.Include(c => c.ComboDetails).ThenInclude(cd => cd.FastFood).ToListAsync();
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SearchName = name;
            return View("Index", results);
        }

        // Fast Food Details
        public async Task<IActionResult> FoodDetails(int id)
        {
            var food = await _context.FastFoods
                .Include(f => f.Category)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (food == null)
            {
                return NotFound();
            }

            return View(food);
        }

        // Combo Details
        public async Task<IActionResult> ComboDetails(int id)
        {
            var combo = await _context.Combos
                .Include(c => c.ComboDetails)
                .ThenInclude(cd => cd.FastFood)
                .ThenInclude(f => f!.Category)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (combo == null)
            {
                return NotFound();
            }

            return View(combo);
        }
    }
}
