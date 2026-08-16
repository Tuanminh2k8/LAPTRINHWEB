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
    [Route("api/orders")]
    [ApiController]
    public class TrackingApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IOrderTrackingService _tracking;
        private readonly IHubContext<OrderTrackingHub> _hub;

        public TrackingApiController(AppDbContext context, IOrderTrackingService tracking, IHubContext<OrderTrackingHub> hub)
        {
            _context = context;
            _tracking = tracking;
            _hub = hub;
        }

        // GET: api/orders/5/tracking — dữ liệu theo dõi thời gian thực (chủ sở hữu / admin / driver)
        [HttpGet("{id:int}/tracking")]
        [Authorize]
        public async Task<ActionResult> GetTracking(int id)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (!userId.HasValue) return Unauthorized(new { message = "Vui lòng đăng nhập." });

            bool isAdmin = User.IsInRole("Admin") || User.IsInRole("Seller");

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Driver)
                .Include(o => o.OrderDetails).ThenInclude(d => d.Modifiers)
                .Include(o => o.OrderDetails).ThenInclude(d => d.FastFood)
                .Include(o => o.OrderDetails).ThenInclude(d => d.Combo)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

            if (order == null) return NotFound(new { message = "Không tìm thấy đơn hàng." });

            bool isOwner = order.UserId == userId.Value;
            bool isDriver = order.Driver != null && order.Driver.UserId == userId.Value;
            if (!isOwner && !isAdmin && !isDriver)
                return Forbid();

            var events = await _context.OrderTrackingEvents
                .AsNoTracking()
                .Where(e => e.OrderId == id)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync();

            var driver = order.Driver;
            bool hasLocation = driver != null && driver.CurrentLat.HasValue && driver.CurrentLng.HasValue;

            return Ok(new
            {
                orderId = order.Id,
                status = order.Status,
                statusLabel = OrderStatus.GetLabel(order.Status),
                icon = OrderStatus.GetIcon(order.Status),
                orderDate = order.OrderDate,
                estimatedDeliveryTime = order.EstimatedDeliveryTime,
                isOwner,
                isDriver,
                driver = driver == null ? null : new
                {
                    driver.Id,
                    driver.FullName,
                    driver.PhoneNumber,
                    driver.AvatarUrl,
                    driver.VehicleType,
                    driver.LicensePlate,
                    driver.Rating,
                    driver.TotalDeliveries,
                    latitude = hasLocation ? driver.CurrentLat : null,
                    longitude = hasLocation ? driver.CurrentLng : null,
                    lastLocationAt = driver.LastLocationAt,
                    hasLocation
                },
                events = events.Select(e => new
                {
                    e.Status,
                    statusLabel = OrderStatus.GetLabel(e.Status),
                    icon = OrderStatus.GetIcon(e.Status),
                    e.Description,
                    e.Actor,
                    e.CreatedAt
                })
            });
        }

        // POST: api/orders/5/assign-driver — Admin gán tài xế cho đơn
        [HttpPost("{id:int}/assign-driver")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDriver(int id, [FromBody] AssignDriverRequest request)
        {
            if (request == null || !request.DriverId.HasValue)
                return BadRequest(new { success = false, message = "Vui lòng chọn tài xế." });

            var order = await _context.Orders
                .Include(o => o.Driver)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

            if (order == null) return NotFound(new { success = false, message = "Không tìm thấy đơn hàng." });

            var driver = await _context.Drivers.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == request.DriverId.Value && d.IsActive);
            if (driver == null) return BadRequest(new { success = false, message = "Tài xế không tồn tại hoặc không hoạt động." });

            // Chỉ gán khi đơn đang sẵn sàng bàn giao (hoặc chưa có tài xế)
            if (order.Status != OrderStatus.ReadyForPickup && order.DriverId == null)
            {
                // Cho phép gán sớm từ Preparing nếu business cho phép
            }

            order.DriverId = driver.Id;
            order.UpdatedAt = DateTime.Now;
            _context.Orders.Update(order);

            _context.OrderTrackingEvents.Add(new OrderTrackingEvent
            {
                OrderId = order.Id,
                Status = OrderStatus.DriverAssigned,
                Description = $"Đã gán tài xế {driver.FullName}",
                Actor = "Admin",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            // Broadcast qua SignalR
            await _hub.Clients.Group($"order-{order.Id}").SendAsync("DriverAssigned", new
            {
                orderId = order.Id,
                driver = new
                {
                    driver.Id,
                    driver.FullName,
                    driver.PhoneNumber,
                    driver.AvatarUrl,
                    driver.VehicleType,
                    driver.LicensePlate,
                    driver.Rating
                },
                at = DateTime.Now
            });

            return Ok(new { success = true, message = $"Đã gán tài xế {driver.FullName} cho đơn #{order.Id}." });
        }

        public class AssignDriverRequest
        {
            public int? DriverId { get; set; }
        }
    }
}