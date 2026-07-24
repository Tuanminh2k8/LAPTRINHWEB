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

        public async Task<IActionResult> Index(string? searchName, int? categoryId, string? sortOrder, int? page)
        {
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = categories;

            var query = _context.FastFoods.Include(f => f.Category).AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(f => f.Name.Contains(searchName));
                ViewBag.SearchName = searchName;
            }

            if (categoryId.HasValue)
            {
                query = query.Where(f => f.CategoryId == categoryId.Value);
                ViewBag.SelectedCategory = categoryId;
            }

            // Sorting
            ViewBag.SortOrder = sortOrder;
            query = sortOrder switch
            {
                "price_asc" => query.OrderBy(f => f.Price),
                "price_desc" => query.OrderByDescending(f => f.Price),
                "name_asc" => query.OrderBy(f => f.Name),
                "name_desc" => query.OrderByDescending(f => f.Name),
                "newest" => query.OrderByDescending(f => f.Id),
                _ => query.OrderBy(f => f.Name)
            };

            // Pagination
            int pageSize = 6;
            int pageNumber = page ?? 1;
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (pageNumber > totalPages && totalPages > 0) pageNumber = totalPages;
            if (pageNumber < 1) pageNumber = 1;

            var foods = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            var combos = await _context.Combos.Include(c => c.ComboDetails).ThenInclude(cd => cd.FastFood).ToListAsync();
            ViewBag.Combos = combos;

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

            ViewBag.RelatedFoods = await _context.FastFoods
                .Include(f => f.Category)
                .Where(f => f.CategoryId == food.CategoryId && f.Id != id)
                .Take(4)
                .ToListAsync();

            return View(food);
        }

        // GET: Home/NotFound
        [HttpGet]
        public IActionResult NotFound(int? statusCode = null)
        {
            ViewBag.StatusCode = statusCode ?? 404;
            return View();
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

            ViewBag.RelatedCombos = await _context.Combos
                .Include(c => c.ComboDetails)
                .ThenInclude(cd => cd.FastFood)
                .Where(c => c.Id != id)
                .Take(3)
                .ToListAsync();

            return View(combo);
        }
    }
}
