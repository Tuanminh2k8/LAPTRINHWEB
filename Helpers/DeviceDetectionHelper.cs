namespace Source.Helpers;

public static class DeviceDetectionHelper
{
    private static readonly HashSet<string> MobileKeywords = new(
    [
        "android", "iphone", "ipad", "ipod", "blackberry", "windows phone",
        "webos", "opera mini", "mobile", "kindle", "silk", "playbook", "bb10",
        "nexus", "pixel", "samsung", "htc", "lg-", "moto", "xiaomi", "huawei"
    ], StringComparer.OrdinalIgnoreCase);

    public const string CookieName = "polyfood_device_mode";
    public const string Mobile = "mobile";
    public const string Desktop = "desktop";

    public static string DetectDeviceType(HttpRequest request)
    {
        var cookie = request.Cookies[CookieName];
        if (!string.IsNullOrWhiteSpace(cookie))
        {
            return cookie.Equals(Mobile, StringComparison.OrdinalIgnoreCase) ? Mobile : Desktop;
        }

        var userAgent = request.Headers.UserAgent.ToString();
        return MobileKeywords.Any(keyword => userAgent.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            ? Mobile : Desktop;
    }
}
