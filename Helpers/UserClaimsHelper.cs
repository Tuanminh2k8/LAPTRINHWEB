using System.Security.Claims;

namespace Source.Helpers
{
    public static class UserClaimsHelper
    {
        public static int? GetUserId(ClaimsPrincipal user)
        {
            var claimId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claimId, out int parsedId) ? parsedId : null;
        }

        public static string? GetFullName(ClaimsPrincipal user) =>
            user.FindFirstValue("FullName") ?? user.Identity?.Name;

        public static string? GetRole(ClaimsPrincipal user) =>
            user.FindFirstValue(ClaimTypes.Role);

        public static bool IsAdmin(ClaimsPrincipal user) =>
            user.IsInRole("Admin");
    }
}
