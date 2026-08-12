using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Helpers;
using Source.Models;
using Source.Services;

namespace Source.Controllers.Api
{
    [Route("api/orders")]
    [ApiController]
    public class OrdersApiController : ControllerBase
    {
        private const string PromoSessionKey = "AppliedPromoCode";
        private const decimal DeliveryFee = 15000;
        private static readonly string[] AllowedPaymentMethods = { "COD", "Bank" };
        private static readonly string[] AllowedOrderTypes = { "Delivery", "Pickup" };

        private readonly AppDbContext _context;
        private readonly ICartSessionService _cartService;
        private readonly IPromoCodeService _promoService;
        private readonly ILogger<OrdersApiController> _logger;

        public OrdersApiController(AppDbContext context, ICartSessionService cartService, IPromoCodeService promoService, ILogger<OrdersApiController> logger)
        {
            _context = context;
            _cartService = cartService;
            _promoService = promoService;
            _logger = logger;
        }

        public class PlaceOrderRequest
        {
            [Required(ErrorMessage = "Tên người nhận không được để trống")]
            [StringLength(100)]
            public string? ReceiverName { get; set; }

            [Required(ErrorMessage = "Số điện thoại không được để trống")]
            [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại Việt Nam không hợp lệ")]
            public string? ReceiverPhone { get; set; }

            // Bắt buộc khi Delivery
            [StringLength(200)]
            public string? ReceiverAddress { get; set; }

            [Required]
            public string? OrderType { get; set; } = "Delivery";

            public DateTime? PickupTime { get; set; }

            [Required]
            public string? PaymentMethod { get; set; } = "COD";

            [StringLength(500)]
            public string? Note { get; set; }

            [StringLength(50)]
            public string? PromoCode { get; set; }
        }

        // POST: api/orders — đặt hàng cho cả khách vãng lai (guest) lẫn đã đăng nhập
        [HttpPost]
        public async Task<ActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)        {
            if (request == null) return BadRequest(new { message = "Dữ liệu đơn hàng không hợp lệ." });

            if (!AllowedOrderTypes.Contains(request.OrderType ?? ""))
            {
                return BadRequest(new { message = "Loại đơn không hợp lệ. Chỉ chấp nhận Delivery hoặc Pickup." });
            }

            request.PaymentMethod = AllowedPaymentMethods.Contains(request.PaymentMethod ?? "") ? request.PaymentMethod : "COD";

            if (request.OrderType == "Delivery" && string.IsNullOrWhiteSpace(request.ReceiverAddress))
            {
                return BadRequest(new { message = "Vui lòng nhập địa chỉ nhận hàng." });
            }

            var cart = _cartService.GetCart();
            if (cart.Count == 0) return BadRequest(new { message = "Giỏ hàng đang trống." });

            // Tính toán lại toàn bộ phía server (không tin client)
            var subtotal = cart.Sum(i => i.TotalPrice);
            var shippingFee = request.OrderType == "Pickup" ? 0m : DeliveryFee;

            var promoResult = await _promoService.ValidateAsync(request.PromoCode, subtotal);
            if (!promoResult.Success && !string.IsNullOrWhiteSpace(request.PromoCode))
            {
                return BadRequest(new { message = promoResult.Message });
            }
            var discount = promoResult.Success ? promoResult.DiscountAmount : 0m;
            var total = subtotal - discount + shippingFee;

            var userId = UserClaimsHelper.GetUserId(User);
            var order = new Order
            {
                UserId = userId, // nullable: guest checkout
                OrderDate = DateTime.Now,
                OrderType = request.OrderType!,
                PickupTime = request.OrderType == "Pickup" ? request.PickupTime : null,
                PaymentMethod = request.PaymentMethod!,
                PaymentStatus = request.PaymentMethod == "Bank" ? "Unpaid" : "Paid",
                Status = OrderStatus.Pending,
                ReceiverName = request.ReceiverName?.Trim() ?? string.Empty,
                ReceiverPhone = request.ReceiverPhone?.Trim() ?? string.Empty,
                ReceiverAddress = request.OrderType == "Delivery" ? request.ReceiverAddress?.Trim() ?? string.Empty : "Tự đến lấy tại cửa hàng",
                Note = request.Note?.Trim(),
                ShippingFee = shippingFee,
                Discount = discount,
                TotalAmount = total,
                UpdatedAt = DateTime.Now
            };

            if (string.IsNullOrWhiteSpace(order.ReceiverName) || string.IsNullOrWhiteSpace(order.ReceiverPhone))
            {
                return BadRequest(new { message = "Vui lòng nhập đầy đủ tên và số điện thoại người nhận." });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in cart)
                {
                    var detail = new OrderDetail
                    {
                        OrderId = order.Id,
                        FastFoodId = item.FastFoodId,
                        ComboId = item.ComboId,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        FastFoodName = item.Name
                    };
                    _context.OrderDetails.Add(detail);
                    await _context.SaveChangesAsync();

                    // Snapshot modifier vào đơn để lịch sử luôn hiển thị đúng
                    foreach (var m in item.Modifiers)
                    {
                        _context.OrderDetailModifiers.Add(new OrderDetailModifier
                        {
                            OrderDetailId = detail.Id,
                            ModifierOptionId = m.OptionId,
                            OptionName = m.OptionName,
                            OptionPrice = m.OptionPrice
                        });
                    }

                    // Tăng SoldCount cho món ăn (không tính combo)
                    if (item.FastFoodId.HasValue)
                    {
                        var food = await _context.FastFoods.FindAsync(item.FastFoodId.Value);
                        if (food != null)
                        {
                            food.SoldCount += item.Quantity;
                            _context.Update(food);
                        }
                    }
                }

                if (promoResult.Success && promoResult.Promo != null)
                {
                    promoResult.Promo.UsedCount++;
                    _context.PromoCodes.Update(promoResult.Promo);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _cartService.ClearCart();
                HttpContext.Session.Remove(PromoSessionKey);

                _logger.LogInformation("API order #{OrderId} placed. Type={Type}, Payment={Pay}, Total={Total}", order.Id, order.OrderType, order.PaymentMethod, order.TotalAmount);

                return Ok(new
                {
                    success = true,
                    orderId = order.Id,
                    orderType = order.OrderType,
                    paymentMethod = order.PaymentMethod,
                    total = order.TotalAmount,
                    subtotal,
                    shippingFee,
                    discount,
                    message = order.PaymentMethod == "Bank"
                        ? "Đặt hàng thành công! Vui lòng chuyển khoản theo mã QR."
                        : "Đặt hàng thành công! Đơn hàng của bạn đang được xử lý."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "API PlaceOrder failed. Items={Count}", cart.Count);
                return StatusCode(500, new { message = "Không thể hoàn tất đặt hàng. Vui lòng thử lại." });
            }
        }

        // GET: api/orders (khách đã đăng nhập) — danh sách đơn của mình
        [HttpGet]
        [Authorize]
        public async Task<ActionResult> MyOrders()
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (!userId.HasValue) return Unauthorized(new { message = "Vui lòng đăng nhập." });

            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.UserId == userId.Value && !o.IsDeleted)
                .OrderByDescending(o => o.OrderDate)
                .Include(o => o.OrderDetails)
                .Select(o => new
                {
                    o.Id,
                    o.OrderDate,
                    o.OrderType,
                    o.Status,
                    statusLabel = OrderStatus.GetLabel(o.Status),
                    o.PaymentMethod,
                    o.TotalAmount,
                    o.ShippingFee,
                    o.Discount,
                    itemCount = o.OrderDetails.Sum(d => d.Quantity),
                    o.PickupTime
                })
                .ToListAsync();

            return Ok(orders);
        }

        // GET: api/orders/5 — chi tiết đơn (chủ sở hữu hoặc admin)
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult> GetOrder(int id)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            bool isAdmin = User.IsInRole("Admin");

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Modifiers)
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.FastFood)
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Combo)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted && (isAdmin || o.UserId == userId));

            if (order == null) return NotFound(new { message = "Không tìm thấy đơn hàng." });

            return Ok(new
            {
                order.Id,
                order.OrderDate,
                order.OrderType,
                order.Status,
                statusLabel = OrderStatus.GetLabel(order.Status),
                order.PaymentMethod,
                order.PaymentStatus,
                order.ReceiverName,
                order.ReceiverPhone,
                order.ReceiverAddress,
                order.PickupTime,
                order.ShippingFee,
                order.Discount,
                order.TotalAmount,
                order.Note,
                order.CancelReason,
                details = order.OrderDetails.Select(d => new
                {
                    d.Id,
                    name = d.FastFoodName ?? d.FastFood?.Name ?? d.Combo?.Name ?? "Sản phẩm",
                    imageUrl = d.FastFood?.ImageUrl ?? d.Combo?.ImageUrl ?? "/images/default_food.jpg",
                    d.Quantity,
                    d.Price,
                    unitTotal = d.Price + d.Modifiers.Sum(m => m.OptionPrice),
                    modifiers = d.Modifiers.Select(m => new { m.OptionName, m.OptionPrice })
                })
            });
        }
    }
}