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
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), HttpOnly = true });
        }
        else if (string.Equals(mode, DeviceDetectionHelper.Desktop, StringComparison.OrdinalIgnoreCase))
        {
            Response.Cookies.Append(DeviceDetectionHelper.CookieName, DeviceDetectionHelper.Desktop,
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), HttpOnly = true });
        }

        var referer = Request.Headers.Referer.ToString();
        if (!string.IsNullOrEmpty(referer))
        {
            return Redirect(referer);
        }

        return Redirect("/");
    }
}
