using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace Source.Services
{
    public interface IPaymentService
    {
        string CreateVnpayPaymentUrl(int orderId, decimal amount, string returnUrl, string ipAddress);
        bool ValidateVnpayReturn(IEnumerable<KeyValuePair<string, string>> query, out int orderId, out string transactionRef, out string message);

        System.Threading.Tasks.Task<(bool success, string? payUrl, string? message)> CreateMomoPaymentAsync(
            int orderId, decimal amount, string returnUrl, string ipnUrl);

        bool ValidateMomoReturn(IEnumerable<KeyValuePair<string, string>> fields, out int orderId, out string transactionRef, out string message);
    }

    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public PaymentService(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        // ───────────────────────── VNPAY ─────────────────────────
        public string CreateVnpayPaymentUrl(int orderId, decimal amount, string returnUrl, string ipAddress)
        {
            var tmnCode = _config["Payment:Vnpay:TmnCode"]!;
            var hashSecret = _config["Payment:Vnpay:HashSecret"]!;
            var baseUrl = _config["Payment:Vnpay:BaseUrl"]!;
            var version = _config["Payment:Vnpay:Version"] ?? "2.1.0";

            var vnp_Params = new SortedDictionary<string, string>(StringComparer.Ordinal);
            vnp_Params.Add("vnp_Version", version);
            vnp_Params.Add("vnp_Command", "pay");
            vnp_Params.Add("vnp_TmnCode", tmnCode);
            vnp_Params.Add("vnp_Locale", "vn");
            vnp_Params.Add("vnp_CurrCode", "VND");
            vnp_Params.Add("vnp_TxnRef", orderId.ToString());
            vnp_Params.Add("vnp_OrderInfo", $"Thanh toan don hang {orderId}");
            vnp_Params.Add("vnp_OrderType", "other");
            vnp_Params.Add("vnp_Amount", ((long)Math.Round(amount * 100)).ToString());
            vnp_Params.Add("vnp_ReturnUrl", returnUrl);
            vnp_Params.Add("vnp_IpAddr", ipAddress);
            vnp_Params.Add("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));

            var query = new StringBuilder();
            foreach (var kv in vnp_Params)
                query.Append($"{kv.Key}={Uri.EscapeDataString(kv.Value)}&");

            var raw = new StringBuilder();
            foreach (var kv in vnp_Params)
                raw.Append($"{kv.Key}={kv.Value}&");

            var secureHash = HmacSha512(hashSecret, raw.ToString().TrimEnd('&'));
            return $"{baseUrl}?{query.ToString().TrimEnd('&')}&vnp_SecureHash={secureHash}";
        }

        public bool ValidateVnpayReturn(IEnumerable<KeyValuePair<string, string>> query, out int orderId, out string transactionRef, out string message)
        {
            orderId = 0;
            transactionRef = "";
            message = "";

            var hashSecret = _config["Payment:Vnpay:HashSecret"]!;
            var sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in query)
            {
                if (string.Equals(kv.Key, "vnp_SecureHash", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                    continue;
                sorted[kv.Key] = kv.Value;
            }

            var raw = new StringBuilder();
            foreach (var kv in sorted)
                raw.Append($"{kv.Key}={kv.Value}&");

            var computed = HmacSha512(hashSecret, raw.ToString().TrimEnd('&'));
            var provided = GetValue(query, "vnp_SecureHash");

            if (!string.Equals(computed, provided, StringComparison.OrdinalIgnoreCase))
            {
                message = "Chữ ký không hợp lệ.";
                return false;
            }

            int.TryParse(GetValue(query, "vnp_TxnRef"), out orderId);
            transactionRef = GetValue(query, "vnp_TransactionNo");

            var responseCode = GetValue(query, "vnp_ResponseCode");
            var txnStatus = GetValue(query, "vnp_TransactionStatus");
            if (responseCode == "00" && txnStatus == "00")
                return true;

            message = $"Thanh toán không thành công (mã {responseCode}).";
            return false;
        }

        // ───────────────────────── MoMo ─────────────────────────
        public async System.Threading.Tasks.Task<(bool success, string? payUrl, string? message)> CreateMomoPaymentAsync(
            int orderId, decimal amount, string returnUrl, string ipnUrl)
        {
            var partnerCode = _config["Payment:Momo:PartnerCode"]!;
            var accessKey = _config["Payment:Momo:AccessKey"]!;
            var secretKey = _config["Payment:Momo:SecretKey"]!;
            var endpoint = _config["Payment:Momo:Endpoint"]!;

            var requestId = Guid.NewGuid().ToString();
            var orderIdStr = orderId.ToString();
            var amountStr = ((long)Math.Round(amount)).ToString();
            var orderInfo = $"Thanh toan don hang {orderId}";
            var requestType = "payWithMethod";
            var extraData = "";

            var rawSignature = $"accessKey={accessKey}&amount={amountStr}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderIdStr}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType={requestType}";
            var signature = HmacSha256(secretKey, rawSignature);

            var payload = new
            {
                partnerCode,
                partnerName = "PolyFood",
                storeId = "PolyFood",
                requestId,
                amount = amountStr,
                orderId = orderIdStr,
                orderInfo,
                redirectUrl = returnUrl,
                ipnUrl,
                requestType,
                extraData,
                signature,
                lang = "vi"
            };

            try
            {
                var client = _httpClientFactory.CreateClient("PaymentGateway");
                client.Timeout = TimeSpan.FromSeconds(20);
                var response = await client.PostAsJsonAsync(endpoint, payload);
                if (!response.IsSuccessStatusCode)
                    return (false, null, "Cổng MoMo không phản hồi.");

                var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                var resultCode = json.GetProperty("resultCode").GetString();
                if (resultCode == "0" || resultCode == "00")
                {
                    var payUrl = json.GetProperty("payUrl").GetString();
                    return (true, payUrl, "");
                }

                var msg = json.TryGetProperty("message", out var m) ? m.GetString() : "Thanh toán MoMo thất bại.";
                return (false, null, msg);
            }
            catch (Exception ex)
            {
                return (false, null, "Lỗi kết nối MoMo: " + ex.Message);
            }
        }

        public bool ValidateMomoReturn(IEnumerable<KeyValuePair<string, string>> fields, out int orderId, out string transactionRef, out string message)
        {
            orderId = 0;
            transactionRef = "";
            message = "";

            var secretKey = _config["Payment:Momo:SecretKey"]!;
            var keys = new[]
            {
                "accessKey", "amount", "extraData", "message", "orderId",
                "orderInfo", "orderType", "partnerCode", "payType",
                "requestId", "responseTime", "resultCode", "transId"
            };

            var raw = new StringBuilder();
            foreach (var k in keys)
                raw.Append($"{k}={GetValue(fields, k)}&");

            var computed = HmacSha256(secretKey, raw.ToString().TrimEnd('&'));
            var provided = GetValue(fields, "signature");

            if (!string.Equals(computed, provided, StringComparison.OrdinalIgnoreCase))
            {
                message = "Chữ ký MoMo không hợp lệ.";
                return false;
            }

            int.TryParse(GetValue(fields, "orderId"), out orderId);
            transactionRef = GetValue(fields, "transId");

            var resultCode = GetValue(fields, "resultCode");
            if (resultCode == "0" || resultCode == "00")
                return true;

            message = "Thanh toán MoMo không thành công.";
            return false;
        }

        // ───────────────────────── Helpers ─────────────────────────
        private static string GetValue(IEnumerable<KeyValuePair<string, string>> collection, string key)
        {
            foreach (var kv in collection)
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            return "";
        }

        private static string HmacSha512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static string HmacSha256(string key, string data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
