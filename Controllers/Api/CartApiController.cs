using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Helpers;
using Source.Models;
using Source.Services;

namespace Source.Controllers.Api
{
    [Route("api/cart")]
    [ApiController]
    public class CartApiController : ControllerBase
    {
        private const string PromoSessionKey = "AppliedPromoCode";
        private readonly AppDbContext _context;
        private readonly ICartSessionService _cartService;
        private readonly IPromoCodeService _promoService;
        private readonly ILoyaltyService _loyalty;

        public CartApiController(AppDbContext context, ICartSessionService cartService, IPromoCodeService promoService, ILoyaltyService loyalty)
        {
            _context = context;
            _cartService = cartService;
            _promoService = promoService;
            _loyalty = loyalty;
        }

        public class AddItemRequest
        {
            public int? FoodId { get; set; }
            public int? ComboId { get; set; }
            public int Quantity { get; set; } = 1;
            // Mảng id của ModifierOption được chọn (chỉ áp dụng cho món ăn, không cho combo)
            public List<int> OptionIds { get; set; } = new();
        }

        // GET: api/cart — nội dung giỏ + tạm tính (kèm modifier snapshot)
        [HttpGet]
        public async Task<ActionResult> GetCart()
        {
            var cart = _cartService.GetCart();
            var subtotal = cart.Sum(i => i.TotalPrice);

            var promoCode = HttpContext.Session.GetString(PromoSessionKey);
            var promoResult = string.IsNullOrEmpty(promoCode)
                ? new PromoValidationResult(false, "", 0, null)
                : await _promoService.ValidateAsync(promoCode, subtotal);

            if (!promoResult.Success) HttpContext.Session.Remove(PromoSessionKey);

            return Ok(new
            {
                items = cart.Select(i => new
                {
                    i.FastFoodId,
                    i.ComboId,
                    i.Name,
                    i.ImageUrl,
                    i.Price,
                    i.Quantity,
                    i.IsCombo,
                    unitTotal = i.UnitPrice,
                    i.TotalPrice,
                    modifiers = i.Modifiers.Select(m => new { m.OptionId, m.OptionName, m.OptionPrice })
                }),
                count = cart.Sum(i => i.Quantity),
                subtotal,
                discount = promoResult.Success ? promoResult.DiscountAmount : 0,
                total = subtotal - (promoResult.Success ? promoResult.DiscountAmount : 0),
                isEmpty = cart.Count == 0
            });
        }

        // POST: api/cart/add — thêm món (có customize) hoặc combo vào giỏ
        [HttpPost("add")]
        public async Task<ActionResult> Add([FromBody] AddItemRequest request)
        {
            if (request == null) return BadRequest(new { message = "Dữ liệu không hợp lệ." });

            var quantity = Math.Clamp(request.Quantity, 1, 50);
            var cart = _cartService.GetCart();

            string name, imageUrl;
            decimal price;
            bool isCombo;
            int? foodId, comboId;
            var chosen = new List<CartItemModifier>();

            if (request.ComboId.HasValue && !request.FoodId.HasValue)
            {
                var combo = await _context.Combos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.ComboId);
                if (combo == null) return NotFound(new { message = "Không tìm thấy combo." });
                name = combo.Name; imageUrl = combo.ImageUrl; price = combo.Price;
                isCombo = true; comboId = combo.Id; foodId = null;
            }
            else if (request.FoodId.HasValue)
            {
                var food = await _context.FastFoods.AsNoTracking()
                    .Include(f => f.ModifierGroups).ThenInclude(g => g.Options)
                    .FirstOrDefaultAsync(f => f.Id == request.FoodId);
                if (food == null) return NotFound(new { message = "Không tìm thấy món ăn." });
                if (!food.IsAvailable) return BadRequest(new { message = "Món ăn này hiện không còn hàng." });

                // Xác thực option ids phía server (chỉ chấp nhận option hợp lệ + thuộc món này)
                if (request.OptionIds.Any())
                {
                    var groups = food.ModifierGroups.ToList();
                    foreach (var group in groups)
                    {
                        var opts = group.Options;
                        var selected = request.OptionIds.Intersect(opts.Select(o => o.Id)).ToList();
                        int takeLimit = group.IsMultiple ? Math.Min(group.MaxOptions, opts.Count) : 1;
                        selected = selected.Take(takeLimit).ToList();
                        if (selected.Count == 0)
                        {
                            var def = opts.FirstOrDefault(o => o.IsDefault) ?? opts.FirstOrDefault();
                            if (def != null) selected = new List<int> { def.Id };
                        }
                        foreach (var optId in selected)
                        {
                            var opt = opts.First(o => o.Id == optId);
                            chosen.Add(new CartItemModifier { OptionId = opt.Id, OptionName = opt.Name, OptionPrice = opt.Price });
                        }
                    }
                }

                name = food.Name; imageUrl = food.ImageUrl; price = food.Price;
                isCombo = false; foodId = food.Id; comboId = null;
            }
            else
            {
                return BadRequest(new { message = "Vui lòng chọn món ăn hoặc combo." });
            }

            var item = cart.FirstOrDefault(i =>
                (isCombo && i.ComboId == comboId) ||
                (!isCombo && i.FastFoodId == foodId && i.Modifiers.Count == chosen.Count &&
                 i.Modifiers.All(m => chosen.Any(c => c.OptionId == m.OptionId))));

            if (item == null)
            {
                cart.Add(new CartItem
                {
                    FastFoodId = foodId,
                    ComboId = comboId,
                    Name = name,
                    ImageUrl = imageUrl,
                    Price = price,
                    Quantity = quantity,
                    IsCombo = isCombo,
                    Modifiers = chosen
                });
            }
            else
            {
                item.Quantity = Math.Min(item.Quantity + quantity, 50);
            }

            _cartService.SaveCart(cart);
            return Ok(new { success = true, message = $"Đã thêm {name} vào giỏ hàng!", count = cart.Sum(i => i.Quantity) });
        }

        [HttpPost("update")]
        public ActionResult UpdateQuantity([FromBody] CartItemRequest request)
        {
            if (request == null || request.Quantity < 0 || request.Quantity > 50)
                return BadRequest(new { message = "Số lượng không hợp lệ." });

            var cart = _cartService.GetCart();
            var item = FindByKey(cart, request);
            if (item == null) return NotFound(new { message = "Không tìm thấy sản phẩm trong giỏ." });

            if (request.Quantity == 0) cart.Remove(item);
            else item.Quantity = request.Quantity;

            _cartService.SaveCart(cart);
            return Ok(new { success = true, count = cart.Sum(i => i.Quantity), total = cart.Sum(i => i.TotalPrice), isEmpty = cart.Count == 0 });
        }

        [HttpPost("remove")]
        public ActionResult Remove([FromBody] CartItemRequest request)
        {
            var cart = _cartService.GetCart();
            var item = FindByKey(cart, request);
            if (item != null) cart.Remove(item);
            _cartService.SaveCart(cart);
            return Ok(new { success = true, count = cart.Sum(i => i.Quantity), total = cart.Sum(i => i.TotalPrice), isEmpty = cart.Count == 0 });
        }

        [HttpPost("clear")]
        public IActionResult Clear()
        {
            _cartService.ClearCart();
            return Ok(new { success = true, message = "Đã làm trống giỏ hàng." });
        }

        [HttpPost("promo")]
        public async Task<ActionResult> ApplyPromo(string? code)
        {
            var cart = _cartService.GetCart();
            if (cart.Count == 0) return BadRequest(new { success = false, message = "Giỏ hàng đang trống." });

            var subtotal = cart.Sum(i => i.TotalPrice);
            var result = await _promoService.ValidateAsync(code, subtotal);
            if (result.Success && result.Promo != null) HttpContext.Session.SetString(PromoSessionKey, result.Promo.Code);
            else HttpContext.Session.Remove(PromoSessionKey);

            return Ok(new
            {
                success = result.Success,
                message = result.Message,
                discount = result.DiscountAmount,
                subtotal,
                total = subtotal - result.DiscountAmount
            });
        }

        [HttpPost("remove-promo")]
        public ActionResult RemovePromo()
        {
            HttpContext.Session.Remove(PromoSessionKey);
            var subtotal = _cartService.GetCart().Sum(i => i.TotalPrice);
            return Ok(new { success = true, message = "Đã bỏ mã giảm giá.", discount = 0, subtotal, total = subtotal });
        }

        [HttpPost("points")]
        public async Task<ActionResult> ApplyPoints(int points)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (!userId.HasValue)
                return BadRequest(new { success = false, message = "Vui lòng đăng nhập để sử dụng điểm thưởng." });

            var cart = _cartService.GetCart();
            if (cart.Count == 0)
                return BadRequest(new { success = false, message = "Giỏ hàng đang trống." });

            var subtotal = cart.Sum(i => i.TotalPrice);
            var promoCode = HttpContext.Session.GetString(PromoSessionKey);
            var promoResult = string.IsNullOrEmpty(promoCode)
                ? new PromoValidationResult(false, "", 0, null)
                : await _promoService.ValidateAsync(promoCode, subtotal);
            var afterPromo = subtotal - (promoResult.Success ? promoResult.DiscountAmount : 0);

            var pr = _loyalty.PreviewRedeem(userId.Value, points, afterPromo);
            if (!pr.ok)
                return Ok(new { success = false, message = pr.message, discount = 0m, pointsUsed = 0 });

            return Ok(new { success = true, message = "Đã áp dụng điểm thưởng.", discount = pr.discount, pointsUsed = pr.pointsUsed, afterPromo });
        }

        private static CartItem? FindByKey(List<CartItem> cart, CartItemRequest r)
        {
            var keyOptionIds = r.OptionIds ?? new List<int>();
            return cart.FirstOrDefault(i =>
                (r.FoodId.HasValue && i.FastFoodId == r.FoodId) ||
                (r.ComboId.HasValue && i.ComboId == r.ComboId)) is var match &&
                match != null &&
                (match.IsCombo || (!match.IsCombo && match.Modifiers.Count == keyOptionIds.Count &&
                    match.Modifiers.All(m => keyOptionIds.Contains(m.OptionId))))
                ? match : null;
        }
    }

    public class CartItemRequest
    {
        public int? FoodId { get; set; }
        public int? ComboId { get; set; }
        public int Quantity { get; set; } = 1;
        public List<int>? OptionIds { get; set; }
    }
}