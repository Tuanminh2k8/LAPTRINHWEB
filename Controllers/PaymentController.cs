using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Helpers;
using Source.Models;
using Source.Services;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace Source.Controllers
{
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPaymentService _payment;

        public PaymentController(AppDbContext context, IPaymentService payment)
        {
            _context = context;
            _payment = payment;
        }

        private static List<KeyValuePair<string, string>> ToStringPairs(IEnumerable<KeyValuePair<string, StringValues>> source)
        {
            return source.Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value.ToString())).ToList();
        }

        // GET: /Payment/VnpayReturn
        public async Task<IActionResult> VnpayReturn()
        {
            var ok = _payment.ValidateVnpayReturn(ToStringPairs(Request.Query), out int orderId, out string txnRef, out string message);

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return NotFound("Không tìm thấy đơn hàng.");

            if (ok && order.PaymentStatus != "Paid")
            {
                order.PaymentStatus = "Paid";
                order.PaymentReference = txnRef;
                order.PaidAt = DateTime.Now;
                order.UpdatedAt = DateTime.Now;
                _context.Update(order);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thanh toán VNPAY thành công!";
            }
            else if (!ok)
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction("Tracking", "Orders", new { id = orderId });
        }

        // GET: /Payment/MomoReturn
        public async Task<IActionResult> MomoReturn()
        {
            var ok = _payment.ValidateMomoReturn(ToStringPairs(Request.Query), out int orderId, out string txnRef, out string message);

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return NotFound("Không tìm thấy đơn hàng.");

            if (ok && order.PaymentStatus != "Paid")
            {
                order.PaymentStatus = "Paid";
                order.PaymentReference = txnRef;
                order.PaidAt = DateTime.Now;
                order.UpdatedAt = DateTime.Now;
                _context.Update(order);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thanh toán MoMo thành công!";
            }
            else if (!ok)
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction("Tracking", "Orders", new { id = orderId });
        }

        // POST: /Payment/MomoIpn  (MoMo server-to-server callback)
        [HttpPost]
        public async Task<IActionResult> MomoIpn()
        {
            var fields = ToStringPairs(Request.Form);
            var ok = _payment.ValidateMomoReturn(fields, out int orderId, out string txnRef, out string message);

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
                return Ok(new { resultCode = "1", message = "Order not found" });

            if (ok && order.PaymentStatus != "Paid")
            {
                order.PaymentStatus = "Paid";
                order.PaymentReference = txnRef;
                order.PaidAt = DateTime.Now;
                order.UpdatedAt = DateTime.Now;
                _context.Update(order);
                await _context.SaveChangesAsync();
            }

            return Ok(new { resultCode = ok ? "0" : "1", message = ok ? "success" : message });
        }
    }
}
