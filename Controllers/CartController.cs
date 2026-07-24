using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Helpers;
using Source.Models;

namespace Source.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        private const string CART_KEY = "FastFoodCart";

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        // Helper method to retrieve current user ID from Session or Cookie claims
        private int? GetCurrentUserId()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue && User.Identity?.IsAuthenticated == true)
            {
                var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(claimId, out int parsedId))
                {
                    userId = parsedId;
                    HttpContext.Session.SetInt32("UserId", parsedId);
                }
            }
            return userId;
        }

        // GET: Cart
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            return View(cart);
        }

        // POST: Cart/AddToCart (AJAX friendly)
        [HttpPost]
        public async Task<IActionResult> AddToCart(int id, bool isCombo, int quantity = 1)
        {
            if (quantity <= 0) quantity = 1;

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
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

            HttpContext.Session.SetObjectAsJson(CART_KEY, cart);

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

        // POST: Cart/UpdateQuantity (AJAX friendly)
        [HttpPost]
        public IActionResult UpdateQuantity(int id, bool isCombo, int quantity)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            var item = cart.FirstOrDefault(i => (isCombo && i.ComboId == id) || (!isCombo && i.FastFoodId == id));

            if (quantity <= 0)
            {
                if (item != null) cart.Remove(item);
            }
            else if (item != null)
            {
                item.Quantity = quantity;
            }

            HttpContext.Session.SetObjectAsJson(CART_KEY, cart);

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

        // POST: Cart/RemoveItem (AJAX friendly)
        [HttpPost]
        public IActionResult RemoveItem(int id, bool isCombo)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            var item = cart.FirstOrDefault(i => (isCombo && i.ComboId == id) || (!isCombo && i.FastFoodId == id));

            if (item != null)
            {
                cart.Remove(item);
                HttpContext.Session.SetObjectAsJson(CART_KEY, cart);
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

        // POST: Cart/ClearCart
        [HttpPost]
        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove(CART_KEY);
            TempData["SuccessMessage"] = "Đã làm trống giỏ hàng!";
            return RedirectToAction("Index");
        }

        // GET: Cart/Checkout
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để thực hiện thanh toán.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });
            }

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
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

        // POST: Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order model)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            if (cart.Count == 0)
            {
                TempData["ErrorMessage"] = "Giỏ hàng đã trống.";
                return RedirectToAction("Index");
            }

            ModelState.Remove("User");
            ModelState.Remove("OrderDetails");

            if (ModelState.IsValid)
            {
                model.UserId = userId.Value;
                model.OrderDate = DateTime.Now;
                model.TotalAmount = cart.Sum(i => i.TotalPrice);
                model.Status = "Pending";

                _context.Orders.Add(model);
                await _context.SaveChangesAsync();

                foreach (var item in cart)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = model.Id,
                        FastFoodId = item.FastFoodId,
                        ComboId = item.ComboId,
                        Quantity = item.Quantity,
                        Price = item.Price
                    };
                    _context.OrderDetails.Add(orderDetail);
                }

                await _context.SaveChangesAsync();

                // Clear Cart
                HttpContext.Session.Remove(CART_KEY);

                TempData["SuccessMessage"] = "Đặt hàng thành công! Đơn hàng của bạn đang được xử lý.";
                return RedirectToAction("OrderTracking", new { id = model.Id });
            }

            ViewBag.Cart = cart;
            return View(model);
        }

        // GET: Cart/OrderHistory
        [HttpGet]
        public async Task<IActionResult> OrderHistory()
        {
            var userId = GetCurrentUserId();
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

        // GET: Cart/OrderTracking/5
        [HttpGet]
        public async Task<IActionResult> OrderTracking(int id)
        {
            var userId = GetCurrentUserId();
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
