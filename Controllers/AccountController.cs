using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IWebHostEnvironment _env;

        public AccountController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var isUsernameTaken = await _context.Users.AnyAsync(u => u.Username.ToLower() == model.Username.ToLower());
                if (isUsernameTaken)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác.");
                }

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

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string identifier = model.UsernameOrEmail.Trim().ToLower();

            // ToLower() cả 2 vế để không phụ thuộc collation của DB
            // (SQL Server mặc định case-insensitive nhưng PostgreSQL/SQLite thì không)
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.Username.ToLower() == identifier || u.Email.ToLower() == identifier);

            if (user == null || !PasswordHelper.VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập / Email hoặc mật khẩu không chính xác.");
                return View(model);
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
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

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

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult GoogleLoginMock()
        {
            // Mock Google login chỉ dành cho môi trường Development (demo).
            if (!_env.IsDevelopment())
            {
                return NotFound();
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GoogleLoginMockSubmit(string email, string name, string subId)
        {
            // Mock Google login chỉ dành cho môi trường Development (demo).
            if (!_env.IsDevelopment())
            {
                return NotFound();
            }

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
                    Username = "google_" + Guid.NewGuid().ToString("N")[..8],
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

            TempData["SuccessMessage"] = $"Đăng nhập Google thành công! Chào {user.FullName}.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = UserClaimsHelper.GetUserId(User);
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

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(User updatedUser, string? newPassword, string? confirmNewPassword)
        {
            var userId = UserClaimsHelper.GetUserId(User);
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

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                if (newPassword.Length < 6)
                {
                    ModelState.AddModelError("newPassword", "Mật khẩu mới phải chứa ít nhất 6 ký tự.");
                }
                else if (newPassword != confirmNewPassword)
                {
                    ModelState.AddModelError("confirmNewPassword", "Mật khẩu xác nhận không trùng khớp.");
                }
            }

            if (ModelState.IsValid)
            {
                var emailTaken = await _context.Users.AnyAsync(u =>
                    u.Id != dbUser.Id && u.Email.ToLower() == updatedUser.Email.Trim().ToLower());
                if (emailTaken)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác.");
                    return View(updatedUser);
                }

                dbUser.FullName = updatedUser.FullName.Trim();
                dbUser.Email = updatedUser.Email.Trim();
                dbUser.PhoneNumber = updatedUser.PhoneNumber.Trim();
                dbUser.Address = updatedUser.Address.Trim();

                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    dbUser.PasswordHash = PasswordHelper.HashPassword(newPassword);
                }

                await _context.SaveChangesAsync();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, dbUser.Id.ToString()),
                    new Claim(ClaimTypes.Name, dbUser.Username),
                    new Claim("FullName", dbUser.FullName),
                    new Claim(ClaimTypes.Email, dbUser.Email),
                    new Claim(ClaimTypes.Role, dbUser.Role)
                };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";
                return View(dbUser);
            }

            return View(updatedUser);
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Bạn đã đăng xuất thành công.";
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/ChangePassword
        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Logout");
            }

            ViewBag.UserName = user.FullName;
            return View();
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ViewModels.ChangePasswordViewModel model)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    return RedirectToAction("Logout");
                }

                if (!PasswordHelper.VerifyPassword(model.CurrentPassword, user.PasswordHash))
                {
                    ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không chính xác.");
                    ViewBag.UserName = user.FullName;
                    return View(model);
                }

                user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
                _context.Update(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return RedirectToAction(nameof(Profile));
            }

            ViewBag.UserName = "Người dùng";
            return View(model);
        }
    }
}
