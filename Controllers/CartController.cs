using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Helpers;
using Source.Models;
using Source.Services;

namespace Source.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICartSessionService _cartService;
        private readonly ILogger<CartController> _logger;

        public CartController(AppDbContext context, ICartSessionService cartService, ILogger<CartController> logger)
        {
            _context = context;
            _cartService = cartService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View(_cartService.GetCart());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int id, bool isCombo, int quantity = 1)
        {
            if (quantity <= 0) quantity = 1;

            var cart = _cartService.GetCart();
            string name = "";
            string imageUrl = "";
            decimal price = 0;

            if (isCombo)
            {
                var combo = await _context.Combos.FindAsync(id);
                if (combo == null) return NotFound();
                name = combo.Name;
                imageUrl = combo.ImageUrl;
                price = combo.Price;
            }
            else
            {
                var food = await _context.FastFoods.FindAsync(id);
                if (food == null) return NotFound();
                name = food.Name;
                imageUrl = food.ImageUrl;
                price = food.Price;
            }

            var item = cart.FirstOrDefault(i => (isCombo && i.ComboId == id) || (!isCombo && i.FastFoodId == id));
            if (item == null)
            {
                cart.Add(new CartItem
                {
                    FastFoodId = isCombo ? null : id,
                    ComboId = isCombo ? id : null,
                    Name = name,
                    ImageUrl = imageUrl,
                    Price = price,
                    Quantity = quantity,
                    IsCombo = isCombo
                });
            }
            else
            {
                item.Quantity += quantity;
            }

            _cartService.SaveCart(cart);

            _logger.LogInformation("User added to cart: {Name} (ID={Id}, IsCombo={IsCombo}, Qty={Qty})", name, id, isCombo, quantity);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    message = $"Đã thêm {name} vào giỏ hàng!",
                    cartCount = cart.Sum(i => i.Quantity),
                    cartTotal = cart.Sum(i => i.TotalPrice)
                });
            }

            TempData["SuccessMessage"] = $"Đã thêm {name} vào giỏ hàng!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(int id, bool isCombo, int quantity)
        {
            var cart = _cartService.GetCart();
            var item = cart.FirstOrDefault(i => (isCombo && i.ComboId == id) || (!isCombo && i.FastFoodId == id));

            if (quantity <= 0)
            {
                if (item != null) cart.Remove(item);
            }
            else if (item != null)
            {
                item.Quantity = quantity;
            }

            _cartService.SaveCart(cart);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    cartCount = cart.Sum(i => i.Quantity),
                    itemTotal = item != null && quantity > 0 ? item.TotalPrice : 0,
                    cartTotal = cart.Sum(i => i.TotalPrice),
                    isEmpty = cart.Count == 0
                });
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveItem(int id, bool isCombo)
        {
            var cart = _cartService.GetCart();
            var item = cart.FirstOrDefault(i => (isCombo && i.ComboId == id) || (!isCombo && i.FastFoodId == id));

            if (item != null)
            {
                cart.Remove(item);
                _cartService.SaveCart(cart);
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    cartCount = cart.Sum(i => i.Quantity),
                    cartTotal = cart.Sum(i => i.TotalPrice),
                    isEmpty = cart.Count == 0
                });
            }

            TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearCart()
        {
            _cartService.ClearCart();
            TempData["SuccessMessage"] = "Đã làm trống giỏ hàng!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });
            }

            var cart = _cartService.GetCart();
            if (cart.Count == 0)
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống. Vui lòng chọn món ăn trước khi thanh toán.";
                return RedirectToAction("Index");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return RedirectToAction("Logout", "Account");

            var order = new Order
            {
                UserId = user.Id,
                ReceiverName = user.FullName,
                ReceiverPhone = user.PhoneNumber,
                ReceiverAddress = user.Address,
                TotalAmount = cart.Sum(i => i.TotalPrice)
            };

            ViewBag.Cart = cart;
            return View(order);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order model)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = _cartService.GetCart();
            if (cart.Count == 0)
            {
                TempData["ErrorMessage"] = "Giỏ hàng đã trống.";
                return RedirectToAction("Index");
            }

            ModelState.Remove("User");
            ModelState.Remove("OrderDetails");

            if (ModelState.IsValid)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    model.UserId = userId.Value;
                    model.OrderDate = DateTime.Now;
                    model.TotalAmount = cart.Sum(i => i.TotalPrice);
                    model.Status = OrderStatus.Pending;

                    _context.Orders.Add(model);
                    await _context.SaveChangesAsync();

                    foreach (var item in cart)
                    {
                        _context.OrderDetails.Add(new OrderDetail
                        {
                            OrderId = model.Id,
                            FastFoodId = item.FastFoodId,
                            ComboId = item.ComboId,
                            Quantity = item.Quantity,
                            Price = item.Price
                        });
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _cartService.ClearCart();

                    TempData["SuccessMessage"] = "Đặt hàng thành công! Đơn hàng của bạn đang được xử lý.";
                    return RedirectToAction("Tracking", "Orders", new { id = model.Id });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Checkout failed for user {UserId}. Cart had {Count} items.", userId.Value, cart.Count);
                    TempData["ErrorMessage"] = "Không thể hoàn tất đặt hàng. Vui lòng thử lại.";
                }
            }

            ViewBag.Cart = cart;
            return View(model);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> OrderHistory()
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == userId.Value)
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> OrderTracking(int id)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.FastFood)
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Combo)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId.Value);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}
