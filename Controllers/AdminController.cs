using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Helpers;
using Source.Models;
using Source.Services;

namespace Source.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private const int PageSize = 15;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AdminController> _logger;
        private readonly ILoyaltyService _loyalty;

        public AdminController(AppDbContext context, IWebHostEnvironment environment, ILogger<AdminController> logger, ILoyaltyService loyalty)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
            _loyalty = loyalty;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.PendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
            ViewBag.DeliveringOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Shipping);
            ViewBag.DeliveredOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Delivered);
            ViewBag.TotalRevenue = await _context.Orders.Where(o => o.Status == OrderStatus.Delivered).SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            ViewBag.TotalFoods = await _context.FastFoods.CountAsync();
            ViewBag.TotalCombos = await _context.Combos.CountAsync();
            ViewBag.TotalUsers = await _context.Users.CountAsync();

            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(6)
                .ToListAsync();

            return View(recentOrders);
        }

        #region User Management

        public async Task<IActionResult> Users(int page = 1)
        {
            page = Math.Max(1, page);
            var query = _context.Users.OrderBy(u => u.Username);
            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            var users = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
            return View(users);
        }

        [HttpGet]
        public IActionResult UserCreate() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserCreate(User user, string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                ModelState.AddModelError("password", "Mật khẩu phải chứa ít nhất 6 ký tự.");
            }

            var existingUser = await _context.Users.AnyAsync(u => u.Username.ToLower() == user.Username.ToLower());
            if (existingUser)
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
            }

            var existingEmail = await _context.Users.AnyAsync(u => u.Email.ToLower() == user.Email.ToLower());
            if (existingEmail)
            {
                ModelState.AddModelError("Email", "Email đã tồn tại");
            }

            ModelState.Remove("PasswordHash");

            // Phân quyền: chỉ chấp nhận role hợp lệ
            if (user.Role != "Admin" && user.Role != "Customer")
            {
                ModelState.AddModelError("Role", "Vai trò không hợp lệ. Chỉ chấp nhận Admin hoặc Customer.");
            }

            if (ModelState.IsValid)
            {
                user.PasswordHash = PasswordHelper.HashPassword(password);
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Admin created user: {Username} (Role={Role})", user.Username, user.Role);
                TempData["SuccessMessage"] = "Thêm người dùng thành công!";
                return RedirectToAction(nameof(Users));
            }
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> UserEdit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserEdit(int id, User updatedUser, string? newPassword)
        {
            if (id != updatedUser.Id) return NotFound();

            var dbUser = await _context.Users.FindAsync(id);
            if (dbUser == null) return NotFound();

            if (dbUser.Username.ToLower() != updatedUser.Username.ToLower())
            {
                var existingUser = await _context.Users.AnyAsync(u => u.Username.ToLower() == updatedUser.Username.ToLower());
                if (existingUser)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                }
            }

            if (dbUser.Email.ToLower() != updatedUser.Email.ToLower())
            {
                var existingEmail = await _context.Users.AnyAsync(u => u.Email.ToLower() == updatedUser.Email.ToLower());
                if (existingEmail)
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại");
                }
            }

            ModelState.Remove("PasswordHash");

            // Phân quyền: role hợp lệ + không cho admin tự hạ quyền chính mình
            if (updatedUser.Role != "Admin" && updatedUser.Role != "Customer")
            {
                ModelState.AddModelError("Role", "Vai trò không hợp lệ. Chỉ chấp nhận Admin hoặc Customer.");
            }

            var currentAdminId = UserClaimsHelper.GetUserId(User);
            if (currentAdminId == id && dbUser.Role == "Admin" && updatedUser.Role != "Admin")
            {
                ModelState.AddModelError("Role", "Bạn không thể tự hạ quyền tài khoản Admin đang đăng nhập.");
            }

            if (ModelState.IsValid)
            {
                dbUser.Username = updatedUser.Username;
                dbUser.FullName = updatedUser.FullName;
                dbUser.Email = updatedUser.Email;
                dbUser.PhoneNumber = updatedUser.PhoneNumber;
                dbUser.Address = updatedUser.Address;
                dbUser.Role = updatedUser.Role;

                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    if (newPassword.Length < 6)
                    {
                        ModelState.AddModelError("newPassword", "Mật khẩu mới phải chứa ít nhất 6 ký tự.");
                        return View(updatedUser);
                    }
                    dbUser.PasswordHash = PasswordHelper.HashPassword(newPassword);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật người dùng thành công!";
                return RedirectToAction(nameof(Users));
            }
            return View(updatedUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserDelete(int id)
        {
            var currentUserId = UserClaimsHelper.GetUserId(User);
            if (currentUserId == id)
            {
                TempData["ErrorMessage"] = "Không thể xóa tài khoản Admin đang đăng nhập!";
                return RedirectToAction(nameof(Users));
            }

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                var hasOrders = await _context.Orders.AnyAsync(o => o.UserId == id);
                if (hasOrders)
                {
                    TempData["ErrorMessage"] = "Không thể xóa người dùng đã có đơn hàng.";
                    return RedirectToAction(nameof(Users));
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa người dùng thành công!";
            }
            return RedirectToAction(nameof(Users));
        }

        #endregion

        #region Category Management

        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .Include(c => c.FastFoods)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(categories);
        }

        [HttpGet]
        public IActionResult CategoryCreate() => View(new Category());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CategoryCreate(Category category)
        {
            var existing = await _context.Categories.AnyAsync(c => c.Name.ToLower() == category.Name.ToLower());
            if (existing)
            {
                ModelState.AddModelError("Name", "Tên danh mục đã tồn tại. Vui lòng chọn tên khác.");
            }

            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Admin created category: {CategoryName}", category.Name);
                TempData["SuccessMessage"] = "Thêm danh mục thành công!";
                return RedirectToAction(nameof(Categories));
            }
            return View(category);
        }

        [HttpGet]
        public async Task<IActionResult> CategoryEdit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CategoryEdit(int id, Category category)
        {
            if (id != category.Id) return NotFound();

            var existing = await _context.Categories.AnyAsync(c => c.Name.ToLower() == category.Name.ToLower() && c.Id != id);
            if (existing)
            {
                ModelState.AddModelError("Name", "Tên danh mục đã tồn tại. Vui lòng chọn tên khác.");
            }

            if (ModelState.IsValid)
            {
                _context.Update(category);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Admin updated category: {CategoryName} (ID={Id})", category.Name, category.Id);
                TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
                return RedirectToAction(nameof(Categories));
            }
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CategoryDelete(int id)
        {
            var category = await _context.Categories.Include(c => c.FastFoods).FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return RedirectToAction(nameof(Categories));

            if (category.FastFoods.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa danh mục đang có món ăn.";
                return RedirectToAction(nameof(Categories));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa danh mục thành công!";
            return RedirectToAction(nameof(Categories));
        }

        #endregion

        #region Fast Food Management

        public async Task<IActionResult> Foods(int page = 1)
        {
            page = Math.Max(1, page);
            var query = _context.FastFoods.Include(f => f.Category).OrderBy(f => f.Name);
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
        public async Task<IActionResult> FoodCreate(FastFood food, IFormFile? imageFile)
        {
            ModelState.Remove("Category");

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

                _context.FastFoods.Add(food);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Admin created food: {FoodName} (Price={Price})", food.Name, food.Price);
                TempData["SuccessMessage"] = "Thêm món ăn thành công!";
                return RedirectToAction(nameof(Foods));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(food);
        }

        [HttpGet]
        public async Task<IActionResult> FoodEdit(int id)
        {
            var food = await _context.FastFoods.FindAsync(id);
            if (food == null) return NotFound();
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(food);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FoodEdit(int id, FastFood food, IFormFile? imageFile)
        {
            if (id != food.Id) return NotFound();
            ModelState.Remove("Category");

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
                var dbFood = await _context.FastFoods.FindAsync(id);
                if (dbFood == null) return NotFound();

                dbFood.Name = food.Name;
                dbFood.Price = food.Price;
                dbFood.Description = food.Description;
                dbFood.CategoryId = food.CategoryId;
                dbFood.Theme = food.Theme;

                if (imageFile != null && imageFile.Length > 0)
                {
                    dbFood.ImageUrl = await ImageUploadHelper.SaveToWwwRootAsync(imageFile, _environment.WebRootPath, "images/uploads")
                        ?? dbFood.ImageUrl;
                }

                _context.Update(dbFood);
                await _context.SaveChangesAsync();
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
            var food = await _context.FastFoods.FindAsync(id);
            if (food != null)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                // Giữ hóa đơn đã phát sinh: OrderDetail đã có Price/FastFoodName là snapshot,
                // nên chỉ ngắt liên kết tới món bị xóa thay vì xóa lịch sử đơn hàng.
                var orderDetails = await _context.OrderDetails
                    .Where(od => od.FastFoodId == id)
                    .ToListAsync();
                foreach (var detail in orderDetails)
                {
                    detail.FastFoodId = null;
                    detail.FastFoodName ??= food.Name;
                    detail.ProductImageUrl ??= food.ImageUrl;
                    detail.ProductDescription ??= food.Description;
                }

                // Combo và mục yêu thích không còn được phép tham chiếu đến món đã xóa.
                var comboDetails = await _context.ComboDetails.Where(cd => cd.FastFoodId == id).ToListAsync();
                var favorites = await _context.FavoriteItems.Where(f => f.FastFoodId == id).ToListAsync();
                _context.ComboDetails.RemoveRange(comboDetails);
                _context.FavoriteItems.RemoveRange(favorites);

                _context.FastFoods.Remove(food);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogWarning("Admin permanently deleted food: {FoodId} ({FoodName})", food.Id, food.Name);
                TempData["SuccessMessage"] = "Đã xóa món ăn và các dữ liệu liên quan. Lịch sử hóa đơn vẫn được giữ lại.";
            }
            return RedirectToAction(nameof(Foods));
        }

        // GET: Admin/ApiFoods - quản lý món ăn (AJAX)
        public async Task<IActionResult> ApiFoods()
        {
            ViewBag.ApiCategories = await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

            ViewBag.ApiFoods = await _context.FastFoods
                .Include(f => f.Category)
                .AsNoTracking()
                .OrderBy(f => f.Name)
                .Select(f => new
                {
                    id = f.Id,
                    name = f.Name,
                    price = f.Price,
                    description = f.Description,
                    imageUrl = f.ImageUrl,
                    theme = f.Theme,
                    categoryId = f.CategoryId,
                    categoryName = f.Category != null ? f.Category.Name : ""
                })
                .ToListAsync();

            ViewBag.ApiCombos = await _context.Combos
                .Include(c => c.ComboDetails)
                .ThenInclude(cd => cd.FastFood)
                .ThenInclude(f => f!.Category)
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    price = c.Price,
                    imageUrl = c.ImageUrl,
                    isOnSale = c.IsOnSale,
                    originalPrice = c.OriginalPrice,
                    items = c.ComboDetails.Select(cd => new
                    {
                        foodName = cd.FastFood != null ? cd.FastFood.Name : "",
                        quantity = cd.Quantity
                    })
                })
                .ToListAsync();

            return View();
        }

        // GET: Admin/Modifiers - quản lý nhóm tùy chọn (size/topping/độ cay) theo món (AJAX)
        public async Task<IActionResult> Modifiers(int? foodId)
        {
            var apiFoods = await _context.FastFoods
                .AsNoTracking()
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

        #endregion

        #region Combo Management

        public async Task<IActionResult> Combos(int page = 1)
        {
            page = Math.Max(1, page);
            var query = _context.Combos
                .Include(c => c.ComboDetails)
                .ThenInclude(cd => cd.FastFood)
                .OrderBy(c => c.Name);

            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            var combos = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
            return View(combos);
        }

        [HttpGet]
        public async Task<IActionResult> ComboCreate()
        {
            ViewBag.Foods = await _context.FastFoods.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ComboCreate(Combo combo, int[] selectedFoods, int[] foodQuantities, IFormFile? imageFile)
        {
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
                    combo.ImageUrl = await ImageUploadHelper.SaveToWwwRootAsync(imageFile, _environment.WebRootPath, "images/uploads")
                        ?? combo.ImageUrl;
                }

                if (selectedFoods != null && selectedFoods.Length > 0)
                {
                    var validFoodIds = await _context.FastFoods.Select(f => f.Id).ToListAsync();
                    var invalidIds = selectedFoods.Where(id => !validFoodIds.Contains(id)).ToArray();
                    if (invalidIds.Any())
                    {
                        ModelState.AddModelError("selectedFoods", "Một số món ăn được chọn không hợp lệ.");
                        ViewBag.Foods = await _context.FastFoods.ToListAsync();
                        return View(combo);
                    }
                }

                _context.Combos.Add(combo);
                await _context.SaveChangesAsync();

                if (selectedFoods != null)
                {
                    for (int i = 0; i < selectedFoods.Length; i++)
                    {
                        var foodId = selectedFoods[i];
                        var quantity = i < foodQuantities.Length ? foodQuantities[i] : 1;

                        _context.ComboDetails.Add(new ComboDetail
                        {
                            ComboId = combo.Id,
                            FastFoodId = foodId,
                            Quantity = quantity
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Thêm combo thành công!";
                return RedirectToAction(nameof(Combos));
            }

            ViewBag.Foods = await _context.FastFoods.ToListAsync();
            return View(combo);
        }

        [HttpGet]
        public async Task<IActionResult> ComboEdit(int id)
        {
            var combo = await _context.Combos
                .Include(c => c.ComboDetails)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (combo == null) return NotFound();

            ViewBag.Foods = await _context.FastFoods.ToListAsync();
            ViewBag.SelectedFoodIds = combo.ComboDetails.Select(cd => cd.FastFoodId).ToArray();
            ViewBag.FoodQuantities = combo.ComboDetails.ToDictionary(cd => cd.FastFoodId, cd => cd.Quantity);

            return View(combo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ComboEdit(int id, Combo combo, int[] selectedFoods, int[] foodQuantities, IFormFile? imageFile)
        {
            if (id != combo.Id) return NotFound();

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
                if (selectedFoods != null && selectedFoods.Length > 0)
                {
                    var validFoodIds = await _context.FastFoods.Select(f => f.Id).ToListAsync();
                    var invalidIds = selectedFoods.Where(id => !validFoodIds.Contains(id)).ToArray();
                    if (invalidIds.Any())
                    {
                        ModelState.AddModelError("selectedFoods", "Một số món ăn được chọn không hợp lệ.");
                        ViewBag.Foods = await _context.FastFoods.ToListAsync();
                        return View(combo);
                    }
                }

                var dbCombo = await _context.Combos.Include(c => c.ComboDetails).FirstOrDefaultAsync(c => c.Id == id);
                if (dbCombo == null) return NotFound();

                dbCombo.Name = combo.Name;
                dbCombo.Price = combo.Price;
                dbCombo.Description = combo.Description;

                if (imageFile != null && imageFile.Length > 0)
                {
                    dbCombo.ImageUrl = await ImageUploadHelper.SaveToWwwRootAsync(imageFile, _environment.WebRootPath, "images/uploads")
                        ?? dbCombo.ImageUrl;
                }

                _context.ComboDetails.RemoveRange(dbCombo.ComboDetails);

                if (selectedFoods != null)
                {
                    for (int i = 0; i < selectedFoods.Length; i++)
                    {
                        var foodId = selectedFoods[i];
                        var quantity = i < foodQuantities.Length ? foodQuantities[i] : 1;

                        _context.ComboDetails.Add(new ComboDetail
                        {
                            ComboId = combo.Id,
                            FastFoodId = foodId,
                            Quantity = quantity
                        });
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật combo thành công!";
                return RedirectToAction(nameof(Combos));
            }

            ViewBag.Foods = await _context.FastFoods.ToListAsync();
            return View(combo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ComboDelete(int id)
        {
            var combo = await _context.Combos.Include(c => c.ComboDetails).FirstOrDefaultAsync(c => c.Id == id);
            if (combo != null)
            {
                var inOrders = await _context.OrderDetails.AnyAsync(od => od.ComboId == id);
                if (inOrders)
                {
                    TempData["ErrorMessage"] = "Không thể xóa combo đang có trong đơn hàng.";
                    return RedirectToAction(nameof(Combos));
                }

                _context.ComboDetails.RemoveRange(combo.ComboDetails);
                _context.Combos.Remove(combo);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa combo thành công!";
            }
            return RedirectToAction(nameof(Combos));
        }

        #endregion

        #region Order Management

        public async Task<IActionResult> Orders(string? status, string? search, string? sort, int? page)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Where(o => !o.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(o =>
                    o.Id.ToString().Contains(search) ||
                    (o.ReceiverName != null && o.ReceiverName.ToLower().Contains(search)) ||
                    (o.ReceiverPhone != null && o.ReceiverPhone.ToLower().Contains(search)) ||
                    (o.User != null && o.User.FullName != null && o.User.FullName.ToLower().Contains(search)));
            }

            ViewBag.Search = search;
            ViewBag.SelectedStatus = status;

            sort = sort?.ToLower();
            query = sort switch
            {
                "oldest" => query.OrderBy(o => o.OrderDate),
                "price_high" => query.OrderByDescending(o => o.TotalAmount),
                "price_low" => query.OrderBy(o => o.TotalAmount),
                _ => query.OrderByDescending(o => o.OrderDate)
            };

            int pageNumber = page ?? 1;
            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));
            if (pageNumber > totalPages && totalPages > 0) pageNumber = totalPages;
            if (pageNumber < 1) pageNumber = 1;

            var orders = await query.Skip((pageNumber - 1) * PageSize).Take(PageSize).ToListAsync();

            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.SortOrder = sort;
            ViewBag.CurrentPage = pageNumber;

            ViewBag.StatusCounts = new Dictionary<string, int>
            {
                { "All", await _context.Orders.CountAsync(o => !o.IsDeleted) },
                { OrderStatus.Pending, await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending && !o.IsDeleted) },
                { OrderStatus.Preparing, await _context.Orders.CountAsync(o => o.Status == OrderStatus.Preparing && !o.IsDeleted) },
                { OrderStatus.Shipping, await _context.Orders.CountAsync(o => o.Status == OrderStatus.Shipping && !o.IsDeleted) },
                { OrderStatus.Delivered, await _context.Orders.CountAsync(o => o.Status == OrderStatus.Delivered && !o.IsDeleted) },
                { OrderStatus.Cancelled, await _context.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled && !o.IsDeleted) },
                { OrderStatus.Refunded, await _context.Orders.CountAsync(o => o.Status == OrderStatus.Refunded && !o.IsDeleted) }
            };

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            if (!OrderStatus.IsValid(status))
            {
                return BadRequest(new { success = false, message = "Trạng thái không hợp lệ." });
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = status;
            if (status == OrderStatus.Delivered) _loyalty.Award(order);
            order.UpdatedAt = DateTime.Now;
            _context.Update(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin updated order #{OrderId} status to: {Status}", id, status);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, newStatus = status, orderId = id });
            }

            TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công!";
            return RedirectToAction(nameof(Orders), new { status });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            return await ChangeStatus(id, OrderStatus.Preparing);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShipOrder(int id)
        {
            return await ChangeStatus(id, OrderStatus.Shipping);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeliverOrder(int id)
        {
            return await ChangeStatus(id, OrderStatus.Delivered);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id, string? cancelReason)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status == OrderStatus.Delivered)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return BadRequest(new { success = false, message = "Không thể hủy đơn hàng đã giao thành công." });
                }
                TempData["ErrorMessage"] = "Không thể hủy đơn hàng đã giao thành công.";
                return RedirectToAction(nameof(Orders));
            }

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.Now;
            order.CancelReason = cancelReason;
            _context.Update(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin cancelled order #{OrderId}. Reason: {Reason}", id, cancelReason);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, newStatus = OrderStatus.Cancelled, orderId = id });
            }

            TempData["SuccessMessage"] = $"Đã hủy đơn hàng #{id}";
            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.IsDeleted = true;
            order.UpdatedAt = DateTime.Now;
            _context.Update(order);
            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, orderId = id });
            }

            TempData["SuccessMessage"] = "Đã xóa đơn hàng thành công!";
            return RedirectToAction(nameof(Orders));
        }

        private async Task<IActionResult> ChangeStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = status;
            if (status == OrderStatus.Delivered) _loyalty.Award(order);
            order.UpdatedAt = DateTime.Now;
            _context.Update(order);
            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, newStatus = status, orderId = id });
            }

            TempData["SuccessMessage"] = $"Đơn hàng #{id} đã chuyển sang trạng thái: {OrderStatus.GetLabel(status)}";
            return RedirectToAction(nameof(Orders));
        }

        // GET: Admin/OrderDetail/5
        public async Task<IActionResult> OrderDetail(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.FastFood)
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Combo)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

            if (order == null) return NotFound();

            ViewBag.StatusBadgeClass = OrderStatus.GetBadgeClass(order.Status);
            ViewBag.StatusLabel = OrderStatus.GetLabel(order.Status);

            var subtotal = order.OrderDetails.Sum(d => d.Price * d.Quantity);
            ViewBag.Subtotal = subtotal;
            ViewBag.GrandTotal = subtotal + order.ShippingFee - order.Discount;

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> PrintInvoice(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.FastFood)
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Combo)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

            if (order == null) return NotFound();

            ViewBag.StatusLabel = OrderStatus.GetLabel(order.Status);
            var subtotal = order.OrderDetails.Sum(d => d.Price * d.Quantity);
            ViewBag.Subtotal = subtotal;
            ViewBag.GrandTotal = subtotal + order.ShippingFee - order.Discount;

            return View(order);
        }

        #endregion

        #region Quản lý chi nhánh & khách hàng thân thiết

        public async Task<IActionResult> Branches()
        {
            var branches = await _context.Branches
                .OrderBy(b => b.SortOrder)
                .ThenBy(b => b.Name)
                .ToListAsync();
            return View(branches);
        }

        [HttpGet]
        public IActionResult BranchCreate() => View(new Branch());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BranchCreate(Branch branch)
        {
            if (!ModelState.IsValid) return View(branch);
            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Branches));
        }

        [HttpGet]
        public async Task<IActionResult> BranchEdit(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null) return NotFound();
            return View(branch);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BranchEdit(int id, Branch branch)
        {
            if (id != branch.Id) return NotFound();
            if (!ModelState.IsValid) return View(branch);
            _context.Update(branch);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Branches));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BranchDelete(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch != null)
            {
                _context.Branches.Remove(branch);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Branches));
        }

        public async Task<IActionResult> Loyalty(int page = 1)
        {
            const int pageSize = 25;
            var transactions = await _context.PointTransactions
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TopUsers = await _context.Users
                .Where(u => u.Points > 0)
                .OrderByDescending(u => u.Points)
                .Take(10)
                .ToListAsync();

            ViewBag.TotalPointsIssued = await _context.PointTransactions
                .Where(t => t.Type == "Earn")
                .SumAsync(t => (long?)t.Points) ?? 0;

            ViewBag.TotalPointsRedeemed = await _context.PointTransactions
                .Where(t => t.Type == "Redeem")
                .SumAsync(t => (long?)t.Points) ?? 0;

            ViewBag.CurrentPage = page;
            ViewBag.HasNext = transactions.Count == pageSize;

            return View(transactions);
        }

        #endregion
    }
}
