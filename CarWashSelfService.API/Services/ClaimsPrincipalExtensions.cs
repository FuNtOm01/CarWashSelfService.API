using System.Security.Claims;

namespace CarWashSelfService.API.Services;

public static class ClaimsPrincipalExtensions
{
    public static int CurrentUserId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static bool IsAdmin(this ClaimsPrincipal principal) =>
        principal.IsInRole("Admin");
}