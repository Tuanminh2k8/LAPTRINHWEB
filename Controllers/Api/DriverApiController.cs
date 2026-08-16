using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Source.Helpers;
using Source.Hubs;
using Source.Models;
using Source.Services;

namespace Source.Controllers.Api
{
    [Route("api/driver")]
    [ApiController]
    [Authorize]
    public class DriverApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<OrderTrackingHub> _hub;
        private readonly IOrderTrackingService _tracking;
        private readonly ILogger<DriverApiController> _logger;

        public DriverApiController(AppDbContext context, IHubContext<OrderTrackingHub> hub, IOrderTrackingService tracking, ILogger<DriverApiController> logger)
        {
            _context = context;
            _hub = hub;
            _tracking = tracking;
            _logger = logger;
        }

        private async Task<Driver?> GetCurrentDriver()
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (!userId.HasValue) return null;
            return await _context.Drivers.AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userId.Value && d.IsActive);
        }

        // GET: api/driver/me — hồ sơ driver + trạng thái online
        [HttpGet("me")]
        public async Task<ActionResult> Me()
        {
            var driver = await GetCurrentDriver();
            if (driver == null) return Unauthorized(new { message = "Tài khoản không phải tài xế." });
            return Ok(new
            {
                driver.Id,
                driver.FullName,
                driver.PhoneNumber,
                driver.AvatarUrl,
                driver.VehicleType,
                driver.LicensePlate,
                driver.Rating,
                driver.TotalDeliveries,
                driver.IsOnline
            });
        }

        // GET: api/driver/orders — đơn đã gán cho driver (active + recent)
        [HttpGet("orders")]
        public async Task<ActionResult> MyOrders()
        {
            var driver = await GetCurrentDriver();
            if (driver == null) return Unauthorized(new { message = "Tài khoản không phải tài xế." });

            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.DriverId == driver.Id && !o.IsDeleted)
                .OrderByDescending(o => o.OrderDate)
                .Take(20)
                .Select(o => new
                {
                    o.Id,
                    o.OrderDate,
                    o.Status,
                    statusLabel = OrderStatus.GetLabel(o.Status),
                    o.ReceiverName,
                    o.ReceiverPhone,
                    o.ReceiverAddress,
                    o.PickupTime,
                    o.TotalAmount,
                    o.PaymentMethod,
                    inDelivery = OrderStatus.InDelivery.Contains(o.Status)
                })
                .ToListAsync();

            return Ok(orders);
        }

        // GET: api/driver/orders/5 — chi tiết đơn cho driver (chỉ đơn đã assign cho mình)
        [HttpGet("orders/{id:int}")]
        public async Task<ActionResult> OrderDetail(int id)
        {
            var driver = await GetCurrentDriver();
            if (driver == null) return Unauthorized(new { message = "Tài khoản không phải tài xế." });

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails).ThenInclude(d => d.Modifiers)
                .Include(o => o.OrderDetails).ThenInclude(d => d.FastFood)
                .FirstOrDefaultAsync(o => o.Id == id && o.DriverId == driver.Id && !o.IsDeleted);

            if (order == null) return NotFound(new { message = "Không tìm thấy đơn hàng được gán cho bạn." });

            return Ok(new
            {
                order.Id,
                order.OrderDate,
                order.Status,
                statusLabel = OrderStatus.GetLabel(order.Status),
                order.ReceiverName,
                order.ReceiverPhone,
                order.ReceiverAddress,
                order.Note,
                order.TotalAmount,
                order.ShippingFee,
                order.PaymentMethod,
                order.PickupTime,
                order.EstimatedDeliveryTime,
                details = order.OrderDetails.Select(d => new
                {
                    name = d.FastFoodName ?? d.FastFood?.Name ?? "Sản phẩm",
                    imageUrl = d.ProductImageUrl ?? d.FastFood?.ImageUrl ?? "/images/default_food.jpg",
                    d.VariantName,
                    d.Quantity,
                    d.Price,
                    unitTotal = d.Price + d.Modifiers.Sum(m => m.OptionPrice),
                    modifiers = d.Modifiers.Select(m => new { m.OptionName, m.OptionPrice })
                })
            });
        }

        // POST: api/driver/orders/5/accept — driver nhận đơn
        [HttpPost("orders/{id:int}/accept")]
        public async Task<IActionResult> AcceptOrder(int id)
        {
            var driver = await GetCurrentDriver();
            if (driver == null) return Unauthorized(new { message = "Tài khoản không phải tài xế." });

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.DriverId == driver.Id);
            if (order == null) return NotFound(new { message = "Không tìm thấy đơn hàng được gán cho bạn." });

            if (order.Status != OrderStatus.DriverAssigned)
                return BadRequest(new { success = false, message = "Đơn hàng không ở trạng thái chờ tài xế nhận." });

            order.DriverAcceptedAt = DateTime.Now;
            var result = await _tracking.TransitionAsync(order, OrderStatus.PickedUp, "Driver", "Tài xế đã nhận đơn và lấy hàng");
            if (!result.ok) return BadRequest(new { success = false, message = result.error });

            await _hub.Clients.Group($"order-{id}").SendAsync("DriverAccepted", new { orderId = id, at = DateTime.Now });

            return Ok(new { success = true, message = "Đã nhận đơn." });
        }

        // POST: api/driver/orders/5/status — driver cập nhật trạng thái (PickedUp → Shipping → Arriving → Delivered)
        [HttpPost("orders/{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Status))
                return BadRequest(new { success = false, message = "Thiếu trạng thái." });

            var driver = await GetCurrentDriver();
            if (driver == null) return Unauthorized(new { message = "Tài khoản không phải tài xế." });

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.DriverId == driver.Id);
            if (order == null) return NotFound(new { message = "Không tìm thấy đơn hàng được gán cho bạn." });

            // Driver chỉ được đưa đơn qua các trạng thái vận chuyển
            if (!OrderStatus.InDelivery.Contains(request.Status) && request.Status != OrderStatus.Delivered)
                return BadRequest(new { success = false, message = "Tài xế không được phép chuyển sang trạng thái này." });

            var result = await _tracking.TransitionAsync(order, request.Status, "Driver", request.Description);
            if (!result.ok) return BadRequest(new { success = false, message = result.error });

            return Ok(new { success = true, message = "Cập nhật trạng thái thành công.", status = order.Status });
        }

        // PUT: api/driver/location — driver gửi vị trí từ browser geolocation
        [HttpPut("location")]
        public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationRequest request)
        {
            if (request == null ||
                !request.Latitude.HasValue || !request.Longitude.HasValue ||
                Math.Abs(request.Latitude.Value) > 90 || Math.Abs(request.Longitude.Value) > 180)
                return BadRequest(new { success = false, message = "Tọa độ không hợp lệ." });

            var userId = UserClaimsHelper.GetUserId(User);
            if (!userId.HasValue) return Unauthorized(new { message = "Vui lòng đăng nhập." });

            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId.Value && d.IsActive);
            if (driver == null) return Unauthorized(new { message = "Tài khoản không phải tài xế." });

            driver.CurrentLat = request.Latitude;
            driver.CurrentLng = request.Longitude;
            driver.LastLocationAt = DateTime.Now;
            _context.Drivers.Update(driver);

            // Broadcast vị trí đến các đơn đang giao của driver này
            var activeOrders = await _context.Orders
                .Where(o => o.DriverId == driver.Id && OrderStatus.InDelivery.Contains(o.Status))
                .Select(o => o.Id)
                .ToListAsync();

            await _context.SaveChangesAsync();

            foreach (var orderId in activeOrders)
            {
                await _hub.Clients.Group($"order-{orderId}").SendAsync("DriverLocationUpdated", new
                {
                    orderId,
                    latitude = driver.CurrentLat,
                    longitude = driver.CurrentLng,
                    at = driver.LastLocationAt
                });
            }

            return Ok(new { success = true, message = "Đã cập nhật vị trí." });
        }

        public class UpdateStatusRequest
        {
            public string? Status { get; set; }
            public string? Description { get; set; }
        }

        public class UpdateLocationRequest
        {
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
        }
    }
}