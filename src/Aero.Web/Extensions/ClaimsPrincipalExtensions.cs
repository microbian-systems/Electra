using System.Security.Claims;

namespace Aero.Common.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetFirstName(this ClaimsPrincipal claimsPrincipal)
        => claimsPrincipal.FindFirstValue(ClaimTypes.Name);

    public static string GetLastName(this ClaimsPrincipal claimsPrincipal)
        => claimsPrincipal.FindFirstValue(ClaimTypes.Surname);

    public static string GetPhoneNumber(this ClaimsPrincipal claimsPrincipal)
        => claimsPrincipal.FindFirstValue(ClaimTypes.MobilePhone);

    // public static string GetUserId(this ClaimsPrincipal claimsPrincipal)
    //    => claimsPrincipal.FindFirstValue("id");
    public static string GetUserId(this ClaimsPrincipal principal)
        => GetUserId<string>(principal);

    public static T GetUserId<T>(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (typeof(T) == typeof(string))
        {
            return (T)Convert.ChangeType(userId, typeof(T));
        }
        else if (typeof(T) == typeof(int) || typeof(T) == typeof(long))
        {
            return userId != null ? (T)Convert.ChangeType(userId, typeof(T)) : (T)Convert.ChangeType(0, typeof(T));
        }
        else if (typeof(T) == typeof(Guid))
        {
            return userId != null ? (T)Convert.ChangeType(userId, typeof(T)) : (T)Convert.ChangeType(0, typeof(T));
        }
        else
        {
            throw new Exception("Invalid type provided");
        }
    }

    public static string GetUserName(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.FindFirstValue(ClaimTypes.Name);
    }

    public static string GetUserEmail(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.FindFirstValue(ClaimTypes.Email);
    }
}