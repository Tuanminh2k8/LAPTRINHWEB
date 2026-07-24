using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Source.Models;
using Source.Helpers;

namespace Source.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // Restrict all actions in this controller to Admin role in session
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang quản trị.";
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
            base.OnActionExecuting(context);
        }

        // GET: Admin/Index (Dashboard)
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.PendingOrders = await _context.Orders.CountAsync(o => o.Status == "Chưa giao");
            ViewBag.DeliveringOrders = await _context.Orders.CountAsync(o => o.Status == "Đang giao");
            ViewBag.DeliveredOrders = await _context.Orders.CountAsync(o => o.Status == "Đã giao");
            ViewBag.TotalRevenue = await _context.Orders.Where(o => o.Status == "Đã giao").SumAsync(o => o.TotalAmount);
            
            ViewBag.TotalFoods = await _context.FastFoods.CountAsync();
            ViewBag.TotalCombos = await _context.Combos.CountAsync();
            ViewBag.TotalUsers = await _context.Users.CountAsync();

            // Recent orders
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            return View(recentOrders);
        }

        #region User Management (CRUD)

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // GET: Admin/UserCreate
        [HttpGet]
        public IActionResult UserCreate()
        {
            return View();
        }

        // POST: Admin/UserCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserCreate(User user)
        {
            var existingUser = await _context.Users.AnyAsync(u => u.Username == user.Username);
            if (existingUser)
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
            }

            if (ModelState.IsValid)
            {
                user.PasswordHash = PasswordHelper.HashPassword(user.PasswordHash);
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm người dùng thành công!";
                return RedirectToAction(nameof(Users));
            }
            return View(user);
        }

        // GET: Admin/UserEdit/5
        [HttpGet]
        public async Task<IActionResult> UserEdit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: Admin/UserEdit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserEdit(int id, User updatedUser, string? newPassword)
        {
            if (id != updatedUser.Id) return NotFound();

            var dbUser = await _context.Users.FindAsync(id);
            if (dbUser == null) return NotFound();

            // Check if username changed and is unique
            if (dbUser.Username != updatedUser.Username)
            {
                var existingUser = await _context.Users.AnyAsync(u => u.Username == updatedUser.Username);
                if (existingUser)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                }
            }

            ModelState.Remove("PasswordHash"); // Password is field updated optionally

            if (ModelState.IsValid)
            {
                dbUser.Username = updatedUser.Username;
                dbUser.FullName = updatedUser.FullName;
                dbUser.Email = updatedUser.Email;
                dbUser.PhoneNumber = updatedUser.PhoneNumber;
                dbUser.Address = updatedUser.Address;
                dbUser.Role = updatedUser.Role;

                if (!string.IsNullOrEmpty(newPassword))
                {
                    dbUser.PasswordHash = PasswordHelper.HashPassword(newPassword);
                }

                _context.Update(dbUser);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật người dùng thành công!";
                return RedirectToAction(nameof(Users));
            }
            return View(updatedUser);
        }

        // POST: Admin/UserDelete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserDelete(int id)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == id)
            {
                TempData["ErrorMessage"] = "Không thể xóa tài khoản Admin đang đăng nhập!";
                return RedirectToAction(nameof(Users));
            }

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa người dùng thành công!";
            }
            return RedirectToAction(nameof(Users));
        }

        #endregion

        #region Fast Food Management (CRUD)

        // GET: Admin/Foods
        public async Task<IActionResult> Foods()
        {
            var foods = await _context.FastFoods.Include(f => f.Category).ToListAsync();
            return View(foods);
        }

        // GET: Admin/FoodCreate
        [HttpGet]
        public async Task<IActionResult> FoodCreate()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View();
        }

        // POST: Admin/FoodCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FoodCreate(FastFood food, IFormFile? imageFile)
        {
            ModelState.Remove("Category");

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                    
                    // Create directory if not exists
                    var dir = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    food.ImageUrl = "/images/" + fileName;
                }

                _context.FastFoods.Add(food);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm món ăn thành công!";
                return RedirectToAction(nameof(Foods));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(food);
        }

        // GET: Admin/FoodEdit/5
        [HttpGet]
        public async Task<IActionResult> FoodEdit(int id)
        {
            var food = await _context.FastFoods.FindAsync(id);
            if (food == null) return NotFound();
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(food);
        }

        // POST: Admin/FoodEdit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FoodEdit(int id, FastFood food, IFormFile? imageFile)
        {
            if (id != food.Id) return NotFound();
            ModelState.Remove("Category");

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
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                    
                    var dir = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    dbFood.ImageUrl = "/images/" + fileName;
                }

                _context.Update(dbFood);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật món ăn thành công!";
                return RedirectToAction(nameof(Foods));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(food);
        }

        // POST: Admin/FoodDelete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FoodDelete(int id)
        {
            var food = await _context.FastFoods.FindAsync(id);
            if (food != null)
            {
                _context.FastFoods.Remove(food);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa món ăn thành công!";
            }
            return RedirectToAction(nameof(Foods));
        }

        #endregion

        #region Combo Management (CRUD)

        // GET: Admin/Combos
        public async Task<IActionResult> Combos()
        {
            var combos = await _context.Combos.Include(c => c.ComboDetails).ThenInclude(cd => cd.FastFood).ToListAsync();
            return View(combos);
        }

        // GET: Admin/ComboCreate
        [HttpGet]
        public async Task<IActionResult> ComboCreate()
        {
            ViewBag.Foods = await _context.FastFoods.ToListAsync();
            return View();
        }

        // POST: Admin/ComboCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ComboCreate(Combo combo, int[] selectedFoods, int[] foodQuantities, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                    
                    var dir = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    combo.ImageUrl = "/images/" + fileName;
                }

                _context.Combos.Add(combo);
                await _context.SaveChangesAsync(); // Saves combo to generate ID

                // Save combo details
                if (selectedFoods != null)
                {
                    for (int i = 0; i < selectedFoods.Length; i++)
                    {
                        var foodId = selectedFoods[i];
                        var quantity = foodQuantities[i];

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

        // GET: Admin/ComboEdit/5
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

        // POST: Admin/ComboEdit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ComboEdit(int id, Combo combo, int[] selectedFoods, int[] foodQuantities, IFormFile? imageFile)
        {
            if (id != combo.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var dbCombo = await _context.Combos.Include(c => c.ComboDetails).FirstOrDefaultAsync(c => c.Id == id);
                if (dbCombo == null) return NotFound();

                dbCombo.Name = combo.Name;
                dbCombo.Price = combo.Price;
                dbCombo.Description = combo.Description;

                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                    
                    var dir = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    dbCombo.ImageUrl = "/images/" + fileName;
                }

                // Remove existing details
                _context.ComboDetails.RemoveRange(dbCombo.ComboDetails);

                // Add new details
                if (selectedFoods != null)
                {
                    for (int i = 0; i < selectedFoods.Length; i++)
                    {
                        var foodId = selectedFoods[i];
                        var quantity = foodQuantities[i];

                        _context.ComboDetails.Add(new ComboDetail
                        {
                            ComboId = combo.Id,
                            FastFoodId = foodId,
                            Quantity = quantity
                        });
                    }
                }

                _context.Update(dbCombo);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật combo thành công!";
                return RedirectToAction(nameof(Combos));
            }

            ViewBag.Foods = await _context.FastFoods.ToListAsync();
            return View(combo);
        }

        // POST: Admin/ComboDelete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ComboDelete(int id)
        {
            var combo = await _context.Combos.Include(c => c.ComboDetails).FirstOrDefaultAsync(c => c.Id == id);
            if (combo != null)
            {
                _context.ComboDetails.RemoveRange(combo.ComboDetails);
                _context.Combos.Remove(combo);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa combo thành công!";
            }
            return RedirectToAction(nameof(Combos));
        }

        #endregion

        #region Order Management

        // GET: Admin/Orders
        public async Task<IActionResult> Orders(string? status)
        {
            var query = _context.Orders.Include(o => o.User).AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            ViewBag.SelectedStatus = status;

            return View(orders);
        }

        // POST: Admin/UpdateOrderStatus (AJAX friendly)
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = status;
            _context.Update(order);
            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, newStatus = status });
            }

            TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công!";
            return RedirectToAction(nameof(Orders), new { status = status });
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
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        #endregion
    }
}
