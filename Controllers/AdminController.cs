using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Helpers;
using Source.Models;

namespace Source.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private const int PageSize = 15;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext context, IWebHostEnvironment environment, ILogger<AdminController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.PendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
            ViewBag.DeliveringOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Delivering);
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
            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
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
                var inOrders = await _context.OrderDetails.AnyAsync(od => od.FastFoodId == id);
                var inCombos = await _context.ComboDetails.AnyAsync(cd => cd.FastFoodId == id);
                if (inOrders || inCombos)
                {
                    TempData["ErrorMessage"] = "Không thể xóa món ăn đang được sử dụng trong combo hoặc đơn hàng.";
                    return RedirectToAction(nameof(Foods));
                }

                _context.FastFoods.Remove(food);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa món ăn thành công!";
            }
            return RedirectToAction(nameof(Foods));
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

        public async Task<IActionResult> Orders(string? status, int page = 1)
        {
            page = Math.Max(1, page);
            var query = _context.Orders.Include(o => o.User).AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));

            ViewBag.SelectedStatus = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

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
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin updated order #{OrderId} status to: {Status}", id, status);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, newStatus = status });
            }

            TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công!";
            return RedirectToAction(nameof(Orders), new { status });
        }

        public async Task<IActionResult> OrderDetail(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.FastFood)
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Combo)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        #endregion
    }
}
