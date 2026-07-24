using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Models;
using Source.Helpers;

namespace Source.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32("UserId").HasValue)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user, string confirmPassword)
        {
            if (user.PasswordHash != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp");
            }

            var existingUser = await _context.Users.AnyAsync(u => u.Username == user.Username);
            if (existingUser)
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
            }

            if (ModelState.IsValid)
            {
                user.PasswordHash = PasswordHelper.HashPassword(user.PasswordHash);
                user.Role = "Customer"; // Guest registers as Customer

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }

            return View(user);
        }

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId").HasValue)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu";
                return View();
            }

            var hashedPassword = PasswordHelper.HashPassword(password);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == hashedPassword);

            if (user != null)
            {
                // Store in session
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("FullName", user.FullName);
                HttpContext.Session.SetString("Role", user.Role);

                TempData["SuccessMessage"] = $"Chào mừng {user.FullName} quay trở lại!";

                if (user.Role == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác";
            return View();
        }

        // GET: Account/GoogleLoginMock
        [HttpGet]
        public IActionResult GoogleLoginMock()
        {
            return View();
        }

        // POST: Account/GoogleLoginMockSubmit
        [HttpPost]
        public async Task<IActionResult> GoogleLoginMockSubmit(string email, string name, string subId)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            // Find or create user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == subId || u.Email == email);
            if (user == null)
            {
                // Create user
                user = new User
                {
                    Username = "google_" + subId.Substring(0, Math.Min(subId.Length, 8)),
                    PasswordHash = PasswordHelper.HashPassword(Guid.NewGuid().ToString()), // Random password
                    FullName = name,
                    Email = email,
                    PhoneNumber = "0900000000",
                    Address = "Địa chỉ liên kết Google",
                    Role = "Customer",
                    GoogleId = subId
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else if (string.IsNullOrEmpty(user.GoogleId))
            {
                user.GoogleId = subId;
                await _context.SaveChangesAsync();
            }

            // Log user in
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("Role", user.Role);

            TempData["SuccessMessage"] = $"Đăng nhập bằng Google thành công! Chào {user.FullName}.";
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Logout");
            }

            return View(user);
        }

        // POST: Account/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(User updatedUser)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue || userId.Value != updatedUser.Id)
            {
                return RedirectToAction("Login");
            }

            // Load user from DB
            var dbUser = await _context.Users.FindAsync(updatedUser.Id);
            if (dbUser == null)
            {
                return NotFound();
            }

            // Update fields (excluding password, username, role, googleid unless admin edit)
            dbUser.FullName = updatedUser.FullName;
            dbUser.Email = updatedUser.Email;
            dbUser.PhoneNumber = updatedUser.PhoneNumber;
            dbUser.Address = updatedUser.Address;

            // Remove model state validations for unchanged/un-editable fields in user profile edit
            ModelState.Remove("Username");
            ModelState.Remove("PasswordHash");

            if (ModelState.IsValid)
            {
                _context.Update(dbUser);
                await _context.SaveChangesAsync();

                // Update session
                HttpContext.Session.SetString("FullName", dbUser.FullName);

                TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";
                return View(dbUser);
            }

            return View(updatedUser);
        }

        // GET: Account/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
