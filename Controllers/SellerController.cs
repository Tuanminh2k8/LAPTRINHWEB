using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Helpers;
using Source.Models;
using Source.Services;

namespace Source.Controllers
{
    [Authorize(Roles = "Seller,Admin")]
    public class SellerController : Controller
    {
        private const int PageSize = 10;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SellerController> _logger;
        private readonly IOrderTrackingService _tracking;

        public SellerController(AppDbContext context, IWebHostEnvironment environment, ILogger<SellerController> logger, IOrderTrackingService tracking)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
            _tracking = tracking;
        }

        // Dashboard dành cho Seller
        public async Task<IActionResult> Index()
        {
            var sellerId = UserClaimsHelper.GetUserId(User);
            if (!sellerId.HasValue) return Challenge();

            // Lấy danh sách ID món ăn của Seller này
            var sellerFoodIds = await _context.FastFoods
                .Where(f => f.SellerId == sellerId.Value)
                .Select(f => f.Id)
                .ToListAsync();

            // Thống kê đơn hàng chứa món của Seller
            var sellerOrdersQuery = _context.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted && o.OrderDetails.Any(d => d.FastFoodId != null && sellerFoodIds.Contains(d.FastFoodId.Value)));

            var orderCounts = await sellerOrdersQuery
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            int GetCount(string s) => orderCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

            ViewBag.TotalOrders = orderCounts.Sum(x => x.Count);
            ViewBag.PendingOrders = GetCount(OrderStatus.Pending);
            ViewBag.PreparingOrders = GetCount(OrderStatus.Preparing);
            ViewBag.ShippingOrders = GetCount(OrderStatus.Shipping);
            ViewBag.DeliveredOrders = GetCount(OrderStatus.Delivered);

            // Tính doanh thu: Tổng tiền các món ăn của Seller trong đơn đã giao thành công (tính ngay trong SQL)
            var totalRevenue = await _context.OrderDetails
                .AsNoTracking()
                .Where(d => d.FastFoodId != null && sellerFoodIds.Contains(d.FastFoodId.Value)
                            && d.Order!.Status == OrderStatus.Delivered && !d.Order.IsDeleted)
                .Select(d => (d.Price + d.Modifiers.Sum(m => m.OptionPrice)) * d.Quantity)
                .SumAsync();

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalFoods = sellerFoodIds.Count;

            var recentOrders = await sellerOrdersQuery
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(6)
                .ToListAsync();

            return View(recentOrders);
        }

        // Quản lý món ăn của Seller
        public async Task<IActionResult> Foods(int page = 1)
        {
            var sellerId = UserClaimsHelper.GetUserId(User);
            if (!sellerId.HasValue) return Challenge();

            page = Math.Max(1, page);
            var query = _context.FastFoods
                .Include(f => f.Category)
                .Where(f => f.SellerId == sellerId.Value)
                .OrderBy(f => f.Name);

            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            var foods = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
            return View(foods);
        }

        [HttpGet]
        public async Task<IActionResult> FoodCreate()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FoodCreate(FastFood food, IFormFile? imageFile, string? variantsJson)
        {
            var sellerId = UserClaimsHelper.GetUserId(User);
            if (!sellerId.HasValue) return Challenge();

            ModelState.Remove("Category");
            ModelState.Remove("Seller");

            if (imageFile != null && imageFile.Length > 0)
            {
                var validation = ImageUploadHelper.Validate(imageFile);
                if (!validation.IsValid)
                {
                    ModelState.AddModelError("imageFile", validation.ErrorMessage!);
                }
            }

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    food.ImageUrl = await ImageUploadHelper.SaveToWwwRootAsync(imageFile, _environment.WebRootPath, "images/uploads")
                        ?? food.ImageUrl;
                }

                food.SellerId = sellerId.Value;
                _context.FastFoods.Add(food);
                await _context.SaveChangesAsync();

                // Save variants
                var variants = ParseVariantsJson(variantsJson);
                foreach (var v in variants)
                {
                    v.FastFoodId = food.Id;
                    _context.FoodVariants.Add(v);
                }
                if (variants.Count > 0)
                {
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Seller {SellerId} created food: {FoodName}", sellerId.Value, food.Name);
                TempData["SuccessMessage"] = "Thêm món ăn thành công!";
                return RedirectToAction(nameof(Foods));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(food);
        }

        [HttpGet]
        public async Task<IActionResult> FoodEdit(int id)
        {
            var sellerId = UserClaimsHelper.GetUserId(User);
            if (!sellerId.HasValue) return Challenge();

            var food = await _context.FastFoods
                .Include(f => f.Variants)
                .FirstOrDefaultAsync(f => f.Id == id && f.SellerId == sellerId.Value);
            if (food == null) return NotFound();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(food);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FoodEdit(int id, FastFood food, IFormFile? imageFile, string? variantsJson)
        {
            var sellerId = UserClaimsHelper.GetUserId(User);
            if (!sellerId.HasValue) return Challenge();

            if (id != food.Id) return NotFound();

            var dbFood = await _context.FastFoods.FirstOrDefaultAsync(f => f.Id == id && f.SellerId == sellerId.Value);
            if (dbFood == null) return NotFound();

            ModelState.Remove("Category");
            ModelState.Remove("Seller");

            if (imageFile != null && imageFile.Length > 0)
            {
                var validation = ImageUploadHelper.Validate(imageFile);
                if (!validation.IsValid)
                {
                    ModelState.AddModelError("imageFile", validation.ErrorMessage!);
                }
            }

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    dbFood.ImageUrl = await ImageUploadHelper.SaveToWwwRootAsync(imageFile, _environment.WebRootPath, "images/uploads")
                        ?? dbFood.ImageUrl;
                }

                dbFood.Name = food.Name;
                dbFood.Price = food.Price;
                dbFood.Description = food.Description;
                dbFood.CategoryId = food.CategoryId;
                dbFood.Theme = food.Theme;
                dbFood.IsAvailable = food.IsAvailable;
                dbFood.IsBestSeller = food.IsBestSeller;

                _context.Update(dbFood);
                await _context.SaveChangesAsync();

                // Sync variants: replace all
                if (variantsJson != null)
                {
                    var oldVariants = await _context.FoodVariants
                        .Where(fv => fv.FastFoodId == id)
                        .ToListAsync();
                    _context.FoodVariants.RemoveRange(oldVariants);

                    var variants = ParseVariantsJson(variantsJson);
                    foreach (var v in variants)
                    {
                        v.Id = 0;
                        v.FastFoodId = id;
                        _context.FoodVariants.Add(v);
                    }
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Seller {SellerId} updated food {FoodId}", sellerId.Value, food.Id);
                TempData["SuccessMessage"] = "Cập nhật món ăn thành công!";
                return RedirectToAction(nameof(Foods));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(food);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FoodDelete(int id)
        {
            var sellerId = UserClaimsHelper.GetUserId(User);
            if (!sellerId.HasValue) return Challenge();

            var food = await _context.FastFoods.FirstOrDefaultAsync(f => f.Id == id && f.SellerId == sellerId.Value);
            if (food == null) return NotFound();

            _context.FastFoods.Remove(food);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa món ăn thành công!";
            return RedirectToAction(nameof(Foods));
        }

        // Quản lý nhóm tùy chọn (Variant/Modifiers)
        public async Task<IActionResult> Modifiers(int? foodId)
        {
            var sellerId = UserClaimsHelper.GetUserId(User);
            if (!sellerId.HasValue) return Challenge();

            var apiFoods = await _context.FastFoods
                .AsNoTracking()
                .Where(f => f.SellerId == sellerId.Value)
                .OrderBy(f => f.Name)
                .Select(f => new
                {
                    id = f.Id,
                    name = f.Name,
                    hasGroups = f.ModifierGroups.Any()
                })
                .ToListAsync();

            ViewBag.ApiFoods = apiFoods;
            ViewBag.SelectedFoodId = foodId is > 0 && apiFoods.Any(f => f.id == foodId)
                ? foodId
                : null;

            return View();
        }

        // Quản lý đơn hàng của Seller
        public async Task<IActionResult> Orders(int page = 1)
        {
            var sellerId = UserClaimsHelper.GetUserId(User);
            if (!sellerId.HasValue) return Challenge();

            var sellerFoodIds = await _context.FastFoods
                .Where(f => f.SellerId == sellerId.Value)
                .Select(f => f.Id)
                .ToListAsync();

            page = Math.Max(1, page);
            var query = _context.Orders
                .Where(o => !o.IsDeleted && o.OrderDetails.Any(d => d.FastFoodId != null && sellerFoodIds.Contains(d.FastFoodId.Value)))
                .OrderByDescending(o => o.OrderDate);

            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            var orders = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> OrderDetail(int id)
        {
            var sellerId = UserClaimsHelper.GetUserId(User);
            if (!sellerId.HasValue) return Challenge();

            var sellerFoodIds = await _context.FastFoods
                .Where(f => f.SellerId == sellerId.Value)
                .Select(f => f.Id)
                .ToListAsync();

            var order = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(d => d.Modifiers)
                .Include(o => o.OrderDetails).ThenInclude(d => d.FastFood)
                .Include(o => o.OrderDetails).ThenInclude(d => d.Combo)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted && o.OrderDetails.Any(d => d.FastFoodId != null && sellerFoodIds.Contains(d.FastFoodId.Value)));

            if (order == null) return NotFound();

            ViewBag.SellerFoodIds = sellerFoodIds;
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var sellerId = UserClaimsHelper.GetUserId(User);
            if (!sellerId.HasValue) return Challenge();

            var sellerFoodIds = await _context.FastFoods
                .Where(f => f.SellerId == sellerId.Value)
                .Select(f => f.Id)
                .ToListAsync();

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted && o.OrderDetails.Any(d => d.FastFoodId != null && sellerFoodIds.Contains(d.FastFoodId.Value)));

            if (order == null) return NotFound();

            if (OrderStatus.IsValidTransition(order.Status, status))
            {
                var result = await _tracking.TransitionAsync(order, status, "Seller", null);
                if (result.ok)
                {
                    _logger.LogInformation("Seller {SellerId} updated order #{OrderId} status to {Status}", sellerId.Value, order.Id, status);
                    TempData["SuccessMessage"] = $"Cập nhật trạng thái đơn hàng #{order.Id} thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = result.error;
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Chuyển đổi trạng thái đơn hàng không hợp lệ.";
            }

            return RedirectToAction(nameof(OrderDetail), new { id });
        }

        private static List<FoodVariant> ParseVariantsJson(string? variantsJson)
        {
            var result = new List<FoodVariant>();
            if (string.IsNullOrWhiteSpace(variantsJson)) return result;

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(variantsJson);
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    var v = new FoodVariant
                    {
                        Name = el.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        Size = el.TryGetProperty("size", out var s) ? s.GetString() ?? "" : "",
                        Color = el.TryGetProperty("color", out var c) ? c.GetString() ?? "" : "",
                        Sku = el.TryGetProperty("sku", out var sku) ? sku.GetString() : null,
                        IsAvailable = el.TryGetProperty("isAvailable", out var avail) ? avail.GetBoolean() : true,
                        IsDefault = el.TryGetProperty("isDefault", out var def) ? def.GetBoolean() : false,
                        StockQuantity = el.TryGetProperty("stock", out var st) ? st.GetInt32() : 0
                    };

                    if (el.TryGetProperty("price", out var p) && p.ValueKind == System.Text.Json.JsonValueKind.Number)
                    {
                        v.Price = p.GetDecimal();
                    }
                    if (el.TryGetProperty("originalPrice", out var op) && op.ValueKind == System.Text.Json.JsonValueKind.Number)
                    {
                        v.OriginalPrice = op.GetDecimal();
                    }
                    if (el.TryGetProperty("imageUrl", out var img))
                    {
                        var imgVal = img.GetString();
                        if (!string.IsNullOrWhiteSpace(imgVal) && !imgVal.StartsWith("data:image"))
                        {
                            v.ImageUrl = imgVal;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(v.Name) && !string.IsNullOrWhiteSpace(v.Size))
                    {
                        result.Add(v);
                    }
                }
            }
            catch
            {
                // Ignore malformed JSON
            }
            return result;
        }
    }
}
