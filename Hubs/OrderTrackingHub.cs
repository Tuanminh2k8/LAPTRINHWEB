using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Source.Helpers;
using Source.Models;

namespace Source.Hubs
{
    /// <summary>
    /// Hub theo dõi đơn hàng thời gian thực.
    /// Customer join nhóm order của chính mình; Driver gửi vị trí cho order đã được assign.
    /// Tuyệt đối không cho client tự broadcast trạng thái — mọi thay đổi phải qua service validate.
    /// </summary>
    [Authorize]
    public class OrderTrackingHub : Hub
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OrderTrackingHub> _logger;

        public OrderTrackingHub(AppDbContext context, ILogger<OrderTrackingHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        private const string OrderGroupPrefix = "order-";

        /// <summary>Customer: tham gia nhóm theo dõi đơn hàng — chỉ khi đơn thuộc chính user.</summary>
        public async Task JoinOrder(int orderId)
        {
            if (Context.User == null) return;

            var userId = UserClaimsHelper.GetUserId(Context.User);
            if (!userId.HasValue) return;

            bool isAdmin = Context.User.IsInRole("Admin");
            bool isSeller = Context.User.IsInRole("Seller");

            var isOwner = await _context.Orders
                .AsNoTracking()
                .AnyAsync(o => o.Id == orderId && o.UserId == userId.Value);

            var isDriver = await _context.Orders
                .AsNoTracking()
                .AnyAsync(o => o.Id == orderId && o.Driver != null && o.Driver.UserId == userId.Value);

            // Seller chỉ join được đơn có chứa món của chính mình (chống rò rỉ realtime đơn khác)
            bool isSellerAccess = isSeller && await _context.Orders
                .AsNoTracking()
                .AnyAsync(o => o.Id == orderId && o.OrderDetails.Any(d => d.FastFood != null && d.FastFood.SellerId == userId.Value));

            if (isOwner || isAdmin || isDriver || isSellerAccess)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, OrderGroupPrefix + orderId);
                _logger.LogInformation("User {UserId} joined order group {OrderId}", userId.Value, orderId);
            }
        }

        /// <summary>Driver: rời nhóm order (khi hoàn thành/không còn phụ trách).</summary>
        public async Task LeaveOrder(int orderId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, OrderGroupPrefix + orderId);
        }

        // NOTE: Không có endpoint client-set status tại đây.
        // Driver gửi location/status qua API riêng (đã authorize + validate transition phía server).
    }
}