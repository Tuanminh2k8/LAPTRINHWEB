using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Models;
using Source.Helpers;

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

            // Check if AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, cartCount = cart.Sum(i => i.Quantity) });
            }

            return RedirectToAction("Index");
        }

        // POST: Cart/UpdateQuantity (AJAX friendly)
        [HttpPost]
        public IActionResult UpdateQuantity(int id, bool isCombo, int quantity)
        {
            if (quantity <= 0)
            {
                return RemoveItem(id, isCombo);
            }

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            var item = cart.FirstOrDefault(i => (isCombo && i.ComboId == id) || (!isCombo && i.FastFoodId == id));

            if (item != null)
            {
                item.Quantity = quantity;
                HttpContext.Session.SetObjectAsJson(CART_KEY, cart);
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { 
                    success = true, 
                    cartCount = cart.Sum(i => i.Quantity),
                    itemTotal = item?.TotalPrice ?? 0,
                    cartTotal = cart.Sum(i => i.TotalPrice)
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
                return Json(new { 
                    success = true, 
                    cartCount = cart.Sum(i => i.Quantity),
                    cartTotal = cart.Sum(i => i.TotalPrice)
                });
            }

            return RedirectToAction("Index");
        }

        // GET: Cart/Checkout
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để đặt hàng.";
                return RedirectToAction("Login", "Account");
            }

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            if (cart.Count == 0)
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
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

            return View(order);
        }

        // POST: Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            if (cart.Count == 0)
            {
                ModelState.AddModelError("", "Giỏ hàng trống");
                return View(model);
            }

            // Remove user validation (since it is set in controller)
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                model.UserId = userId.Value;
                model.OrderDate = DateTime.Now;
                model.TotalAmount = cart.Sum(i => i.TotalPrice);
                model.Status = "Chưa giao";

                _context.Orders.Add(model);
                await _context.SaveChangesAsync(); // Saves order and generates order ID

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

                // Clear cart
                HttpContext.Session.Remove(CART_KEY);

                TempData["SuccessMessage"] = "Đặt hàng thành công!";
                return RedirectToAction("OrderTracking", new { id = model.Id });
            }

            return View(model);
        }

        // GET: Cart/OrderHistory
        [HttpGet]
        public async Task<IActionResult> OrderHistory()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == userId.Value)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // GET: Cart/OrderTracking
        [HttpGet]
        public async Task<IActionResult> OrderTracking(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
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
