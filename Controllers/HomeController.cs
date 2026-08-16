using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Models;
using Source.Services;

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

        public async Task<IActionResult> Index(string? searchName, int? categoryId, string? sortOrder, int? page)
        {
            page = Math.Max(1, page ?? 1);
            var categories = await _context.Categories.AsNoTracking().ToListAsync();
            ViewBag.Categories = categories;

            var query = _context.FastFoods.Include(f => f.Category).AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
            {
                searchName = searchName.Trim();
                if (searchName.Length > 100) searchName = searchName[..100];
                query = query.Where(f => f.Name.Contains(searchName));
                ViewBag.SearchName = searchName;
            }

            if (categoryId.HasValue)
            {
                query = query.Where(f => f.CategoryId == categoryId.Value);
                ViewBag.SelectedCategory = categoryId;
            }

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

            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));

            var currentPage = page.Value;
            if (currentPage > totalPages)
            {
                currentPage = totalPages;
            }

            var foods = await query
                .Skip((currentPage - 1) * PageSize)
                .Take(PageSize)
                .AsNoTracking()
                .ToListAsync();

            var combos = await _context.Combos.Include(c => c.ComboDetails).ThenInclude(cd => cd.FastFood).AsNoTracking().ToListAsync();
            ViewBag.Combos = combos;
            ViewBag.PageNumber = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.CurrentPage = currentPage;

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

            var results = await query.AsNoTracking().ToListAsync();

            // Check if AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_FoodListPartial", results);
            }

            ViewBag.Categories = await _context.Categories.AsNoTracking().ToListAsync();
            ViewBag.Combos = await _context.Combos.Include(c => c.ComboDetails).ThenInclude(cd => cd.FastFood).AsNoTracking().ToListAsync();
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SearchName = name;
            return View("Index", results);
        }

        // Fast Food Details
        public async Task<IActionResult> FoodDetails(int id)
        {
            var food = await _context.FastFoods
                .Include(f => f.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);

            if (food == null)
            {
                return NotFound();
            }

            ViewBag.RelatedFoods = await _context.FastFoods
                .Include(f => f.Category)
                .AsNoTracking()
                .Where(f => f.CategoryId == food.CategoryId && f.Id != id)
                .Take(4)
                .ToListAsync();

            return View(food);
        }

        // Home/NotFound — chấp nhận mọi verb: UseStatusCodePagesWithReExecute
        // re-execute request lỗi với method gốc (POST lỗi -> POST NotFound)
        [AcceptVerbs("GET", "POST", "PUT", "DELETE")]
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
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (combo == null)
            {
                return NotFound();
            }

            ViewBag.RelatedCombos = await _context.Combos
                .Include(c => c.ComboDetails)
                .ThenInclude(cd => cd.FastFood)
                .AsNoTracking()
                .Where(c => c.Id != id)
                .Take(3)
                .ToListAsync();

            return View(combo);
        }

        public async Task<IActionResult> Menu(int? categoryId, int page = 1)
        {
            const int pageSize = 12;
            var categories = await _context.Categories.AsNoTracking().ToListAsync();
            ViewBag.Categories = categories;

            var query = _context.FastFoods.Include(f => f.Category).AsNoTracking().AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(f => f.CategoryId == categoryId.Value);
                ViewBag.SelectedCategory = categoryId;
            }

            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var foods = await query
                .OrderBy(f => f.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.PageNumber = page;
            ViewBag.TotalPages = totalPages;

            return View(foods);
        }

        public async Task<IActionResult> Combos()
        {
            var combos = await _context.Combos
                .Include(c => c.ComboDetails)
                .ThenInclude(cd => cd.FastFood)
                .AsNoTracking()
                .ToListAsync();
            return View(combos);
        }

        public IActionResult Promotions() => View();

        public IActionResult About() => View();

        public IActionResult Privacy() => View();

        // Demo vòng đời DI (Singleton / Scoped / Transient)
        public IActionResult DependencyInjectionDemo()
        {
            var scoped1 = HttpContext.RequestServices.GetRequiredService<IScopedOperation>();
            var transient1 = HttpContext.RequestServices.GetRequiredService<ITransientOperation>();
            var singleton1 = HttpContext.RequestServices.GetRequiredService<ISingletonOperation>();

            var transient2 = HttpContext.RequestServices.GetRequiredService<ITransientOperation>();

            ViewBag.SingletonId = singleton1.OperationId;
            ViewBag.ScopedId = scoped1.OperationId;
            ViewBag.Transient1Id = transient1.OperationId;
            ViewBag.Transient2Id = transient2.OperationId;

            return View();
        }

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
