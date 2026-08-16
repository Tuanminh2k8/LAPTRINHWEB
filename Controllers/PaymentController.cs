using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Source.Helpers;
using Source.Models;
using Source.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Primitives;

namespace Source.Controllers
{
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPaymentService _payment;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(AppDbContext context, IPaymentService payment, ILogger<PaymentController> logger)
        {
            _context = context;
            _payment = payment;
            _logger = logger;
        }

        private static List<KeyValuePair<string, string>> ToStringPairs(IEnumerable<KeyValuePair<string, StringValues>> source)
        {
            return source.Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value.ToString())).ToList();
        }

        private async Task<bool> MarkPaidAsync(Order order, string txnRef)
        {
            if (order.PaymentStatus == "Paid") return true;

            order.PaymentStatus = "Paid";
            order.PaymentReference = txnRef;
            order.PaidAt = DateTime.Now;
            order.UpdatedAt = DateTime.Now;
            _context.Update(order);
            await _context.SaveChangesAsync();
            return true;
        }

        // GET: /Payment/VnpayReturn
        public async Task<IActionResult> VnpayReturn()
        {
            var ok = _payment.ValidateVnpayReturn(ToStringPairs(Request.Query), out int orderId, out string txnRef, out string message);

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return NotFound("Không tìm thấy đơn hàng.");

            // Verify số tiền (vnp_Amount tính bằng đồng × 100) — phòng bị chỉnh sửa
            if (ok && long.TryParse(Request.Query["vnp_Amount"].ToString(), out long paidAmount)
                && paidAmount != (long)Math.Round(order.TotalAmount * 100))
            {
                _logger.LogWarning("VNPAY amount mismatch for order #{OrderId}: paid {Paid}, expected {Expected}", order.Id, paidAmount, (long)Math.Round(order.TotalAmount * 100));
                ok = false;
                message = "Số tiền thanh toán không khớp với đơn hàng.";
            }

            if (ok)
            {
                await MarkPaidAsync(order, txnRef);
                TempData["SuccessMessage"] = "Thanh toán VNPAY thành công!";
            }
            else
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

            // Verify số tiền
            if (ok && decimal.TryParse(Request.Query["amount"].ToString(), out decimal paidAmount)
                && paidAmount != order.TotalAmount)
            {
                _logger.LogWarning("MoMo amount mismatch for order #{OrderId}: paid {Paid}, expected {Expected}", order.Id, paidAmount, order.TotalAmount);
                ok = false;
                message = "Số tiền thanh toán không khớp với đơn hàng.";
            }

            if (ok)
            {
                await MarkPaidAsync(order, txnRef);
                TempData["SuccessMessage"] = "Thanh toán MoMo thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction("Tracking", "Orders", new { id = orderId });
        }

        // Đọc callback MoMo IPN: hỗ trợ cả form-encoded lẫn JSON body
        private async Task<List<KeyValuePair<string, string>>> ReadIpnFieldsAsync()
        {
            var contentType = Request.ContentType ?? "";
            if (contentType.Contains("application/json", System.StringComparison.OrdinalIgnoreCase))
            {
                var fields = new List<KeyValuePair<string, string>>();
                using var doc = await JsonDocument.ParseAsync(Request.Body);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    fields.Add(new KeyValuePair<string, string>(prop.Name, prop.Value.ToString()));
                }
                return fields;
            }

            return ToStringPairs(Request.Form);
        }

        // POST: /Payment/MomoIpn  (MoMo server-to-server callback)
        [HttpPost]
        public async Task<IActionResult> MomoIpn()
        {
            var fields = await ReadIpnFieldsAsync();
            var ok = _payment.ValidateMomoReturn(fields, out int orderId, out string txnRef, out string message);

            if (!ok)
            {
                _logger.LogWarning("MoMo IPN invalid signature: {Message}", message);
                return Ok(new { resultCode = "1", message = "Invalid signature" });
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
                return Ok(new { resultCode = "1", message = "Order not found" });

            // Verify số tiền
            if (decimal.TryParse(GetValue(fields, "amount"), out decimal paidAmount) && paidAmount != order.TotalAmount)
            {
                _logger.LogWarning("MoMo IPN amount mismatch for order #{OrderId}", order.Id);
                return Ok(new { resultCode = "1", message = "Amount mismatch" });
            }

            await MarkPaidAsync(order, txnRef);
            return Ok(new { resultCode = "0", message = "success" });
        }

        private static string GetValue(IEnumerable<KeyValuePair<string, string>> fields, string key)
        {
            foreach (var kv in fields)
                if (string.Equals(kv.Key, key, System.StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            return "";
        }
    }
}