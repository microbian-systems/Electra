using System.Globalization;
using System.Text.RegularExpressions;
using Aero.Common.Constants;

namespace Aero.Core.Extensions;

public static class StringExtensions
{
    public static string FromByteArray(this byte[] bytes)
        => Encoding.ASCII.GetString(bytes);
    
    public static byte[] ToByteArray(this string str)
        => Encoding.ASCII.GetBytes(str);
    public static string ToBase64(this string str) 
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
    
    public static string FromBase64(this string str) 
        => Encoding.UTF8.GetString(Convert.FromBase64String(str));
    
    public static string ToBase64(this byte[] bytes) 
        => Convert.ToBase64String(bytes);
    
    public static byte[] FromBase64ToBytes(this string str)
        => Convert.FromBase64String(str);
    public static bool IsNullOrEmpty(this string s) => string.IsNullOrEmpty(s);
        
    public static string ToTitleCase(this string word) =>
        CultureInfo.CurrentCulture.TextInfo.ToTitleCase(word.ToLower());
        
    public static bool IsValidEmail(this string email) => RegExMatch(email, RegExConstants.Email);

    private static bool RegExMatch(string val, string pattern) => Regex.Match(val, pattern).Success;
        
    public static string ToCamelCase(this string str)
    {
        var pattern = new Regex(@"[A-Z]{2,}(?=[A-Z][a-z]+[0-9]*|\b)|[A-Z]?[a-z]+[0-9]*|[A-Z]|[0-9]+");
        return new string(
            new CultureInfo("en-US", false)
                .TextInfo
                .ToTitleCase(
                    string.Join(" ", pattern.Matches(str)).ToLower()
                )
                .Replace(@" ", "")
                .Select((x, i) => i == 0 ? char.ToLower(x) : x)
                .ToArray()
        );
    }
}