using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Helpers;
using Source.Models;
using Source.ViewModels;

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
            if (User.Identity?.IsAuthenticated == true || HttpContext.Session.GetInt32("UserId").HasValue)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check duplicate Username
                var isUsernameTaken = await _context.Users.AnyAsync(u => u.Username.ToLower() == model.Username.ToLower());
                if (isUsernameTaken)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác.");
                }

                // Check duplicate Email
                var isEmailTaken = await _context.Users.AnyAsync(u => u.Email.ToLower() == model.Email.ToLower());
                if (isEmailTaken)
                {
                    ModelState.AddModelError("Email", "Địa chỉ Email này đã được đăng ký tài khoản khác.");
                }

                if (ModelState.IsValid)
                {
                    var newUser = new User
                    {
                        Username = model.Username.Trim(),
                        FullName = model.FullName.Trim(),
                        Email = model.Email.Trim(),
                        PhoneNumber = model.PhoneNumber.Trim(),
                        Address = model.Address.Trim(),
                        PasswordHash = PasswordHelper.HashPassword(model.Password),
                        Role = "Customer"
                    };

                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập với thông tin vừa tạo.";
                    return RedirectToAction("Login");
                }
            }

            return View(model);
        }

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true || HttpContext.Session.GetInt32("UserId").HasValue)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string identifier = model.UsernameOrEmail.Trim().ToLower();

            var user = await _context.Users.FirstOrDefaultAsync(u => 
                u.Username.ToLower() == identifier || u.Email.ToLower() == identifier);

            if (user == null || !PasswordHelper.VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập / Email hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Create Authentication Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FullName", user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Store user details in Session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("Role", user.Role);

            TempData["SuccessMessage"] = $"Chào mừng {user.FullName} đã đăng nhập thành công!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/GoogleLoginMock
        [HttpGet]
        public IActionResult GoogleLoginMock()
        {
            return View();
        }

        // POST: Account/GoogleLoginMockSubmit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GoogleLoginMockSubmit(string email, string name, string subId)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Không thể lấy thông tin Email từ Google.";
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == subId || u.Email.ToLower() == email.ToLower());
            if (user == null)
            {
                user = new User
                {
                    Username = "google_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    PasswordHash = PasswordHelper.HashPassword(Guid.NewGuid().ToString()),
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

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FullName", user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("Role", user.Role);

            TempData["SuccessMessage"] = $"Đăng nhập Google thành công! Chào {user.FullName}.";
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue && User.Identity?.IsAuthenticated == true)
            {
                var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(claimId, out int parsedId)) userId = parsedId;
            }

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
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue && User.Identity?.IsAuthenticated == true)
            {
                var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(claimId, out int parsedId)) userId = parsedId;
            }

            if (!userId.HasValue || userId.Value != updatedUser.Id)
            {
                return RedirectToAction("Login");
            }

            var dbUser = await _context.Users.FindAsync(updatedUser.Id);
            if (dbUser == null)
            {
                return NotFound();
            }

            ModelState.Remove("Username");
            ModelState.Remove("PasswordHash");

            if (ModelState.IsValid)
            {
                dbUser.FullName = updatedUser.FullName.Trim();
                dbUser.Email = updatedUser.Email.Trim();
                dbUser.PhoneNumber = updatedUser.PhoneNumber.Trim();
                dbUser.Address = updatedUser.Address.Trim();

                _context.Update(dbUser);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("FullName", dbUser.FullName);

                TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";
                return View(dbUser);
            }

            return View(updatedUser);
        }

        // GET/POST: Account/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Bạn đã đăng xuất thành công.";
            return RedirectToAction("Index", "Home");
        }
    }
}
