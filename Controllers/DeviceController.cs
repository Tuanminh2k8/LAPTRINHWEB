using Microsoft.AspNetCore.Mvc;
using Source.Helpers;

namespace Source.Controllers;

public class DeviceController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("SetMode")]
    public IActionResult SetModePost(string mode)
    {
        return SetModeCore(mode);
    }

    [HttpGet]
    [ActionName("SetMode")]
    public IActionResult SetModeGet(string mode)
    {
        return SetModeCore(mode);
    }

    private RedirectResult SetModeCore(string mode)
    {
        if (string.Equals(mode, DeviceDetectionHelper.Mobile, StringComparison.OrdinalIgnoreCase))
        {
            Response.Cookies.Append(DeviceDetectionHelper.CookieName, DeviceDetectionHelper.Mobile,
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), HttpOnly = true, SameSite = SameSiteMode.Lax });
        }
        else if (string.Equals(mode, DeviceDetectionHelper.Desktop, StringComparison.OrdinalIgnoreCase))
        {
            Response.Cookies.Append(DeviceDetectionHelper.CookieName, DeviceDetectionHelper.Desktop,
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), HttpOnly = true, SameSite = SameSiteMode.Lax });
        }

        // Open-redirect guard: chỉ redirect về Referer NẾU cùng host với request (chống dẫn user sang trang độc hại)
        var referer = Request.Headers.Referer.ToString();
        if (!string.IsNullOrEmpty(referer)
            && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri)
            && string.Equals(refererUri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
        {
            return Redirect(referer);
        }

        return Redirect("/");
    }
}
