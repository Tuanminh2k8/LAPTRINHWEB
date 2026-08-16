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
        private const string PromoSessionKey = "AppliedPromoCode";
        private readonly AppDbContext _context;
        private readonly ICartSessionService _cartService;
        private readonly IPromoCodeService _promoService;
        private readonly IPromotionService _promotionService;
        private readonly IPaymentService _payment;
        private readonly IConfiguration _config;
        private readonly ILogger<CartController> _logger;

        public CartController(AppDbContext context, ICartSessionService cartService, IPromoCodeService promoService, IPromotionService promotionService, IPaymentService payment, IConfiguration config, ILogger<CartController> logger)
        {
            _context = context;
            _cartService = cartService;
            _promoService = promoService;
            _promotionService = promotionService;
            _payment = payment;
            _config = config;
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
            if (quantity > 50) quantity = 50;

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
                item.Quantity = Math.Min(item.Quantity + quantity, 50);
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
                item.Quantity = Math.Min(quantity, 50);
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
            HttpContext.Session.Remove(PromoSessionKey);
            TempData["SuccessMessage"] = "Đã làm trống giỏ hàng!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyPromo(string? promoCode)
        {
            var cart = _cartService.GetCart();
            var subtotal = cart.Sum(i => i.TotalPrice);

            if (cart.Count == 0)
            {
                HttpContext.Session.Remove(PromoSessionKey);
                return Json(new { success = false, message = "Giỏ hàng đang trống, không thể áp dụng mã.", discount = 0, subtotal = 0, total = 0 });
            }

            var result = await _promoService.ValidateAsync(promoCode, subtotal);

            if (result.Success && result.Promo != null)
            {
                HttpContext.Session.SetString(PromoSessionKey, result.Promo.Code);
            }
            else
            {
                HttpContext.Session.Remove(PromoSessionKey);
            }

            return Json(new
            {
                success = result.Success,
                message = result.Message,
                discount = result.DiscountAmount,
                subtotal,
                total = subtotal - result.DiscountAmount
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemovePromo()
        {
            HttpContext.Session.Remove(PromoSessionKey);
            var cart = _cartService.GetCart();
            var subtotal = cart.Sum(i => i.TotalPrice);
            return Json(new { success = true, message = "Đã bỏ mã giảm giá.", discount = 0, subtotal, total = subtotal });
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = _cartService.GetCart();
            if (cart.Count == 0)
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống. Vui lòng chọn món ăn trước khi thanh toán.";
                return RedirectToAction("Index");
            }

            var userId = UserClaimsHelper.GetUserId(User);
            User? user = userId.HasValue ? await _context.Users.FindAsync(userId.Value) : null;

            var subtotal = cart.Sum(i => i.TotalPrice);
            var promoCode = HttpContext.Session.GetString(PromoSessionKey);
            var promoResult = await _promoService.ValidateAsync(promoCode, subtotal);
            if (!promoResult.Success) HttpContext.Session.Remove(PromoSessionKey);

            var order = new Order
            {
                UserId = user?.Id,
                ReceiverName = user?.FullName ?? string.Empty,
                ReceiverPhone = user?.PhoneNumber ?? string.Empty,
                ReceiverAddress = user?.Address ?? string.Empty,
                Discount = promoResult.Success ? promoResult.DiscountAmount : 0,
                TotalAmount = subtotal - (promoResult.Success ? promoResult.DiscountAmount : 0),
                PromoCode = promoResult.Success ? promoResult.Promo?.Code : null,
                Tax = 0m
            };

            ViewBag.Cart = cart;
            ViewBag.Subtotal = subtotal;
            ViewBag.PromoCode = promoResult.Success ? promoResult.Promo!.Code : null;
            ViewBag.PromoDiscount = promoResult.Success ? promoResult.DiscountAmount : 0m;
            ViewBag.Branches = await _context.Branches.AsNoTracking().ToListAsync();
            ViewBag.UserPoints = user?.Points ?? 0;
            return View(order);
        }

        private static readonly string[] AllowedPaymentMethods = { "COD", "Bank", "VNPay", "MoMo" };

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout([Bind("ReceiverName,ReceiverPhone,ReceiverAddress,PaymentMethod,Note")] Order model)
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

            if (string.IsNullOrWhiteSpace(model.PaymentMethod) || !AllowedPaymentMethods.Contains(model.PaymentMethod))
            {
                model.PaymentMethod = "COD";
            }

            if (ModelState.IsValid)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var subtotal = cart.Sum(i => i.TotalPrice);

                    // Xác thực mã giảm giá phía server (không tin client)
                    var promoCode = HttpContext.Session.GetString(PromoSessionKey);
                    decimal discount = 0m;
                    PromoValidationResult? promoValidation = null;
                    if (!string.IsNullOrWhiteSpace(promoCode))
                    {
                        promoValidation = await _promoService.ValidateAsync(promoCode, subtotal);
                        discount = promoValidation.Success ? promoValidation.DiscountAmount : 0m;
                    }

                    model.UserId = userId.Value;
                    model.OrderDate = DateTime.Now;
                    model.Discount = discount;
                    model.TotalAmount = subtotal - discount;
                    model.Status = OrderStatus.Pending;

_context.Orders.Add(model);
                    await _context.SaveChangesAsync();

                    // Batch-load dữ liệu món/combo để tránh N+1 query trong vòng lặp
                    var foodIds = cart.Where(i => i.FastFoodId.HasValue).Select(i => i.FastFoodId!.Value).Distinct().ToList();
                    var comboIds = cart.Where(i => i.ComboId.HasValue).Select(i => i.ComboId!.Value).Distinct().ToList();

                    var foods = await _context.FastFoods
                        .AsNoTracking()
                        .Include(f => f.Seller)
                        .Where(f => foodIds.Contains(f.Id))
                        .ToDictionaryAsync(f => f.Id);

                    var combos = await _context.Combos
                        .AsNoTracking()
                        .Where(c => comboIds.Contains(c.Id))
                        .ToDictionaryAsync(c => c.Id);

                    var comboFirstFoods = await _context.ComboDetails
                        .AsNoTracking()
                        .Include(cd => cd.FastFood).ThenInclude(f => f!.Seller)
                        .Where(cd => comboIds.Contains(cd.ComboId))
                        .GroupBy(cd => cd.ComboId)
                        .Select(g => new { ComboId = g.Key, Food = g.Select(x => x.FastFood).FirstOrDefault() })
                        .ToDictionaryAsync(x => x.ComboId, x => x.Food);

                    foreach (var item in cart)
                    {
                        var sellerName = "";
                        string? productDescription = null;

                        if (item.FastFoodId.HasValue && foods.TryGetValue(item.FastFoodId.Value, out var food))
                        {
                            sellerName = food.Seller?.FullName ?? "";
                            productDescription = food.Description;
                        }
                        else if (item.ComboId.HasValue)
                        {
                            if (combos.TryGetValue(item.ComboId.Value, out var combo))
                            {
                                productDescription = combo.Description;
                            }
                            if (comboFirstFoods.TryGetValue(item.ComboId.Value, out var firstFood))
                            {
                                sellerName = firstFood?.Seller?.FullName ?? "";
                            }
                        }

                        var detail = new OrderDetail
                        {
                            OrderId = model.Id,
                            FastFoodId = item.FastFoodId,
                            ComboId = item.ComboId,
                            Quantity = item.Quantity,
                            Price = item.Price,
                            FastFoodName = item.Name,
                            ProductImageUrl = item.ImageUrl,
                            Sku = item.Sku,
                            VariantName = item.VariantName,
                            SellerName = sellerName,
                            ProductDescription = productDescription
                        };

                        _context.OrderDetails.Add(detail);
                    }

                    // Ghi nhận lượt dùng (concurrency-safe, trong cùng transaction)
                    if (promoValidation != null && promoValidation.Success && promoValidation.Promo != null)
                    {
                        var usage = await _promotionService.RedeemAsync(
                            promoValidation.Promo.Id, userId.Value, model.Id, subtotal, discount);
                        if (usage == null)
                        {
                            await transaction.RollbackAsync();
                            HttpContext.Session.Remove(PromoSessionKey);
                            TempData["ErrorMessage"] = "Mã giảm giá không còn khả dụng. Vui lòng thử lại.";
                            return RedirectToAction("Checkout");
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _cartService.ClearCart();
                    HttpContext.Session.Remove(PromoSessionKey);

                    if (model.PaymentMethod == "Bank")
                    {
                        TempData["SuccessMessage"] = "Đặt hàng thành công! Vui lòng chuyển khoản theo hướng dẫn bên dưới.";
                        return RedirectToAction("BankTransfer", "Orders", new { id = model.Id });
                    }

                    if (model.PaymentMethod == "VNPay" || model.PaymentMethod == "MoMo")
                    {
                        // Gọi cổng thanh toán NGOÀI transaction (tránh giữ transaction DB lâu khi chờ HTTP).
                        var returnUrl = $"{Request.Scheme}://{Request.Host}{_config["Payment:Vnpay:ReturnPath"]}";
                        string? paymentUrl = null;
                        if (model.PaymentMethod == "VNPay")
                        {
                            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                            paymentUrl = _payment.CreateVnpayPaymentUrl(model.Id, model.TotalAmount, returnUrl, ip);
                        }
                        else
                        {
                            var ipnUrl = $"{Request.Scheme}://{Request.Host}{_config["Payment:Momo:IpnPath"]}";
                            var momo = await _payment.CreateMomoPaymentAsync(model.Id, model.TotalAmount, returnUrl, ipnUrl);
                            if (!momo.success || string.IsNullOrEmpty(momo.payUrl))
                            {
                                TempData["ErrorMessage"] = "Đặt hàng thành công nhưng không khởi tạo được thanh toán online. Vui lòng thử lại hoặc thanh toán COD.";
                                return RedirectToAction("Tracking", "Orders", new { id = model.Id });
                            }
                            paymentUrl = momo.payUrl;
                        }

                        if (!string.IsNullOrWhiteSpace(paymentUrl))
                        {
                            return Redirect(paymentUrl);
                        }
                    }

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
            {
                var vbSubtotal = cart.Sum(i => i.TotalPrice);
                var vbPromoCode = HttpContext.Session.GetString(PromoSessionKey);
                var vbPromo = await _promoService.ValidateAsync(vbPromoCode, vbSubtotal);
                ViewBag.Subtotal = vbSubtotal;
                ViewBag.PromoCode = vbPromo.Success ? vbPromo.Promo!.Code : null;
                ViewBag.PromoDiscount = vbPromo.Success ? vbPromo.DiscountAmount : 0m;
            }
            return View(model);
        }

        // Gộp trang: Cart/OrderHistory & Cart/OrderTracking trùng lặp với Orders/Index & Orders/Tracking.
        // Giữ redirect 301 để không gãy link cũ (bookmark, lịch sử trình duyệt).
        [HttpGet]
        public IActionResult OrderHistory()
        {
            return RedirectToActionPermanent("Index", "Orders");
        }

        [HttpGet]
        public IActionResult OrderTracking(int id)
        {
            return RedirectToActionPermanent("Tracking", "Orders", new { id });
        }
    }
}
