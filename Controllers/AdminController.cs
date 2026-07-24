using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Source.Helpers;
using Source.Models;

namespace Source.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // Restrict all actions in this controller to Admin role in session or Claims
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var sessionRole = HttpContext.Session.GetString("Role");
            var isClaimAdmin = User.IsInRole("Admin") || User.FindFirstValue(ClaimTypes.Role) == "Admin";

            if (sessionRole != "Admin" && !isClaimAdmin)
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
            ViewBag.TotalRevenue = await _context.Orders.Where(o => o.Status == "Đã giao").SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            
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

            ViewBag.SelectedStatus = status;
            ViewBag.Search = search;

            sort = sort?.ToLower();
            query = sort switch
            {
                "oldest" => query.OrderBy(o => o.OrderDate),
                "price_high" => query.OrderByDescending(o => o.TotalAmount),
                "price_low" => query.OrderBy(o => o.TotalAmount),
                _ => query.OrderByDescending(o => o.OrderDate)
            };

            int pageSize = 10;
            int pageNumber = page ?? 1;
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (pageNumber > totalPages && totalPages > 0) pageNumber = totalPages;
            if (pageNumber < 1) pageNumber = 1;

            var orders = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.SortOrder = sort;

            ViewBag.StatusCounts = new Dictionary<string, int>
            {
                { "All", await _context.Orders.CountAsync(o => !o.IsDeleted) },
                { "Pending", await _context.Orders.CountAsync(o => o.Status == "Pending" && !o.IsDeleted) },
                { "Preparing", await _context.Orders.CountAsync(o => o.Status == "Preparing" && !o.IsDeleted) },
                { "Shipping", await _context.Orders.CountAsync(o => o.Status == "Shipping" && !o.IsDeleted) },
                { "Delivered", await _context.Orders.CountAsync(o => o.Status == "Delivered" && !o.IsDeleted) },
                { "Cancelled", await _context.Orders.CountAsync(o => o.Status == "Cancelled" && !o.IsDeleted) },
                { "Refunded", await _context.Orders.CountAsync(o => o.Status == "Refunded" && !o.IsDeleted) }
            };

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            var allowed = new[] { "Pending", "Preparing", "Shipping", "Delivered", "Cancelled", "Refunded" };
            if (!allowed.Contains(status)) return BadRequest();

            order.Status = status;
            order.UpdatedAt = DateTime.Now;
            _context.Update(order);
            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, newStatus = status, orderId = id });
            }

            TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công!";
            return RedirectToAction(nameof(Orders), new { status = status });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            return await ChangeStatus(id, "Preparing");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShipOrder(int id)
        {
            return await ChangeStatus(id, "Shipping");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeliverOrder(int id)
        {
            return await ChangeStatus(id, "Delivered");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            return await ChangeStatus(id, "Cancelled");
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
            order.UpdatedAt = DateTime.Now;
            _context.Update(order);
            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, newStatus = status, orderId = id });
            }

            TempData["SuccessMessage"] = $"Đơn hàng #{id} đã chuyển sang trạng thái: {GetStatusLabel(status)}";
            return RedirectToAction(nameof(Orders));
        }

        private string GetStatusLabel(string status)
        {
            return status switch
            {
                "Pending" => "Chờ xác nhận",
                "Preparing" => "Đang chuẩn bị",
                "Shipping" => "Đang giao",
                "Delivered" => "Đã giao",
                "Cancelled" => "Đã hủy",
                "Refunded" => "Hoàn tiền",
                _ => status
            };
        }

        private string GetStatusBadgeClass(string status)
        {
            return status switch
            {
                "Pending" => "bg-warning text-dark",
                "Preparing" => "bg-info text-dark",
                "Shipping" => "bg-primary",
                "Delivered" => "bg-success",
                "Cancelled" => "bg-danger",
                "Refunded" => "bg-secondary",
                _ => "bg-secondary"
            };
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

            ViewBag.StatusBadgeClass = GetStatusBadgeClass(order.Status);
            ViewBag.StatusLabel = GetStatusLabel(order.Status);

            var subtotal = order.OrderDetails.Sum(d => d.Price * d.Quantity);
            ViewBag.Subtotal = subtotal;
            ViewBag.GrandTotal = subtotal + order.ShippingFee - order.Discount;

            return View(order);
        }

        #endregion
    }
}
