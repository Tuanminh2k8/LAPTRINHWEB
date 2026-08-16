using Microsoft.AspNetCore.SignalR;
using Source.Hubs;
using Source.Models;

namespace Source.Services
{
    /// <summary>
    /// Dịch vụ điều phối trạng thái đơn hàng: validate transition, ghi tracking event,
    /// cập nhật timestamp, và broadcast qua SignalR. Single source of truth.
    /// </summary>
    public interface IOrderTrackingService
    {
        /// <summary>
        /// Chuyển trạng thái đơn hàng với validation theo state machine.
        /// actor: System | Seller | Driver | Admin | Customer
        /// </summary>
        Task<(bool ok, string? error)> TransitionAsync(
            Order order,
            string targetStatus,
            string actor,
            string? description = null,
            bool allowCancellation = true);
    }

    public class OrderTrackingService : IOrderTrackingService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<OrderTrackingHub> _hub;

        public OrderTrackingService(AppDbContext context, IHubContext<OrderTrackingHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        public async Task<(bool ok, string? error)> TransitionAsync(
            Order order,
            string targetStatus,
            string actor,
            string? description = null,
            bool allowCancellation = true)
        {
            if (order == null) return (false, "Không tìm thấy đơn hàng.");

            if (order.Status == targetStatus)
                return (false, "Đơn hàng đã ở trạng thái này.");

            // Cho phép hủy (chỉ từ các trạng thái sớm, do customer/seller/admin)
            if (targetStatus == OrderStatus.Cancelled && !allowCancellation)
                return (false, "Không được phép hủy đơn hàng ở trạng thái này.");

            if (targetStatus == OrderStatus.Cancelled)
            {
                if (order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
                    return (false, "Chỉ có thể hủy đơn hàng khi chưa bắt đầu chuẩn bị.");
            }
            else if (!OrderStatus.IsValidTransition(order.Status, targetStatus))
            {
                return (false, $"Không thể chuyển từ \"{OrderStatus.GetLabel(order.Status)}\" sang \"{OrderStatus.GetLabel(targetStatus)}\".");
            }

            // Cập nhật trạng thái + timestamp tương ứng
            var now = DateTime.Now;
            order.Status = targetStatus;
            order.UpdatedAt = now;
            switch (targetStatus)
            {
                case OrderStatus.Confirmed: order.ConfirmedAt = now; break;
                case OrderStatus.ReadyForPickup: order.ReadyAt = now; break;
                case OrderStatus.PickedUp: order.PickedUpAt = now; break;
                case OrderStatus.Delivered: order.DeliveredAt = now; break;
            }

            _context.Orders.Update(order);

            _context.OrderTrackingEvents.Add(new OrderTrackingEvent
            {
                OrderId = order.Id,
                Status = targetStatus,
                Description = description ?? OrderStatus.GetLabel(targetStatus),
                Actor = actor,
                CreatedAt = now
            });

            await _context.SaveChangesAsync();

            // Broadcast qua SignalR
            await _hub.Clients.Group($"order-{order.Id}").SendAsync("OrderStatusChanged", new
            {
                orderId = order.Id,
                status = targetStatus,
                statusLabel = OrderStatus.GetLabel(targetStatus),
                icon = OrderStatus.GetIcon(targetStatus),
                actor,
                at = now
            });

            if (targetStatus == OrderStatus.Delivered)
            {
                await _hub.Clients.Group($"order-{order.Id}").SendAsync("OrderDelivered", new { orderId = order.Id });
            }

            return (true, null);
        }
    }
}