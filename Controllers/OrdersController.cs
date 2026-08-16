using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Helpers;
using Source.Models;
using Source.Services;

namespace Source.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OrdersController> _logger;
        private readonly IOrderTrackingService _tracking;

        public OrdersController(AppDbContext context, ILogger<OrdersController> logger, IOrderTrackingService tracking)
        {
            _context = context;
            _logger = logger;
            _tracking = tracking;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            const int PageSize = 10;
            page = Math.Max(1, page);

            var query = _context.Orders
                .Where(o => o.UserId == userId.Value && !o.IsDeleted)
                .OrderByDescending(o => o.OrderDate)
                .AsNoTracking();

            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));

            if (page > totalPages) page = totalPages;

            var orders = await query
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.FastFood)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Combo)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.FastFood)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Combo)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Modifiers)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId.Value && !o.IsDeleted);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // Guest có thể theo dõi đơn bằng số điện thoại đặt hàng (không cần tài khoản).
        [AllowAnonymous]
        public IActionResult GuestTrack(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> Tracking(int id, string? phone = null)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (userId.HasValue)
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.FastFood)
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Combo)
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Modifiers)
                    .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId.Value);

                if (order == null)
                {
                    return NotFound();
                }

                return View(order);
            }

            // Guest: cần số điện thoại trùng khớp với SĐT đặt hàng mới được xem
            if (string.IsNullOrWhiteSpace(phone))
            {
                return RedirectToAction(nameof(GuestTrack), new { id });
            }

            var guestOrder = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.FastFood)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Combo)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Modifiers)
                .FirstOrDefaultAsync(o => o.Id == id && o.ReceiverPhone == phone.Trim());

            if (guestOrder == null)
            {
                ViewBag.OrderId = id;
                TempData["ErrorMessage"] = "Số điện thoại không khớp với đơn hàng.";
                return RedirectToAction(nameof(GuestTrack), new { id });
            }

            // Guest đơn chuyển khoản chưa thanh toán: đưa thẳng tới hướng dẫn chuyển khoản
            if (guestOrder.PaymentMethod == "Bank" && guestOrder.PaymentStatus != "Paid")
            {
                return RedirectToAction(nameof(BankTransfer), new { id, phone = phone.Trim() });
            }

            return View(guestOrder);
        }

        // GET: Orders/BankTransfer/5 — hướng dẫn chuyển khoản cho đơn thanh toán Bank
        [AllowAnonymous]
        public async Task<IActionResult> BankTransfer(int id, string? phone = null)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            Order? order;
            if (userId.HasValue)
            {
                order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId.Value && !o.IsDeleted);
            }
            else
            {
                // Guest: cần SĐT trùng khớp
                if (string.IsNullOrWhiteSpace(phone))
                {
                    return RedirectToAction(nameof(GuestTrack), new { id });
                }
                order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == id && o.ReceiverPhone == phone.Trim() && !o.IsDeleted);
            }

            if (order == null)
            {
                return NotFound();
            }

            if (order.PaymentMethod != "Bank")
            {
                return RedirectToAction("Tracking", new { id });
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string cancelReason)
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (string.IsNullOrWhiteSpace(cancelReason))
            {
                if (isAjax) return Json(new { success = false, message = "Vui lòng nhập lý do hủy đơn hàng." });
                TempData["ErrorMessage"] = "Vui lòng nhập lý do hủy đơn hàng.";
                return RedirectToAction("Details", new { id });
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId.Value);

            if (order == null)
            {
                if (isAjax) return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
                return NotFound();
            }

            if (order.Status != OrderStatus.Pending)
            {
                if (isAjax) return Json(new { success = false, message = "Chỉ có thể hủy đơn hàng khi đang ở trạng thái Chờ xác nhận." });
                TempData["ErrorMessage"] = "Chỉ có thể hủy đơn hàng khi đang ở trạng thái Chờ xác nhận.";
                return RedirectToAction("Details", new { id });
            }

            try
            {
                var result = await _tracking.TransitionAsync(order, OrderStatus.Cancelled, "Customer", cancelReason);
                if (!result.ok)
                {
                    if (isAjax) return Json(new { success = false, message = result.error });
                    TempData["ErrorMessage"] = result.error;
                    return RedirectToAction("Details", new { id });
                }
                order.CancelReason = cancelReason;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Customer cancelled order #{OrderId}. Reason: {Reason}", id, cancelReason);

                if (isAjax) return Json(new { success = true, redirect = Url.Action("Details", new { id }) });

                TempData["SuccessMessage"] = "Hủy đơn hàng thành công!";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order #{OrderId}", id);
                if (isAjax) return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
                TempData["ErrorMessage"] = "Có lỗi xảy ra. Vui lòng thử lại.";
                return RedirectToAction("Details", new { id });
            }
        }
    }
}