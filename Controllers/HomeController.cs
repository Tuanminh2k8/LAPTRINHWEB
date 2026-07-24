using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Models;

namespace Source.Controllers
{
    public class HomeController : Controller
    {
        private const int PageSize = 12;
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(AppDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? searchName, int? categoryId, int page = 1)
        {
            page = Math.Max(1, page);
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = categories;

            var query = _context.FastFoods.Include(f => f.Category).AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
            {
                searchName = searchName.Trim();
                if (searchName.Length > 100) searchName = searchName[..100];
                query = query.Where(f => f.Name.Contains(searchName));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(f => f.CategoryId == categoryId.Value);
            }

            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));

            if (page > totalPages)
            {
                page = totalPages;
            }

            var foods = await query
                .OrderBy(f => f.Name)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var combos = await _context.Combos.Include(c => c.ComboDetails).ThenInclude(cd => cd.FastFood).ToListAsync();

            ViewBag.Combos = combos;
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SearchName = searchName;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(foods);
        }

        // Advanced Search (AJAX friendly)
        public async Task<IActionResult> AdvancedSearch(string? name, decimal? minPrice, decimal? maxPrice, int? categoryId, string? theme, string? description)
        {
            var query = _context.FastFoods.Include(f => f.Category).AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                name = name.Trim();
                if (name.Length > 100) name = name[..100];
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
                theme = theme.Trim();
                if (theme.Length > 100) theme = theme[..100];
                query = query.Where(f => f.Theme.Contains(theme));
            }

            if (!string.IsNullOrEmpty(description))
            {
                description = description.Trim();
                if (description.Length > 250) description = description[..250];
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

        public IActionResult Privacy() => View();

        public IActionResult PageNotFound(int? statusCode)
        {
            if (statusCode.HasValue && statusCode.Value == 404)
            {
                Response.StatusCode = 404;
                return View();
            }
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
