namespace Aero.Common.Constants;

public static class RegExConstants
{
    public const string Email = @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$";

    public const string Phone = @"^\+?1?\s*\(?\d{3}\)?[\s.\-]?\d{3}[\s.\-]?\d{4}$";

    // E.164 format: +14155552671 (strict international standard)
    public const string PhoneE164 = @"^\+[1-9]\d{6,14}$";

    // Accepts: +44 20 7946 0958 / +1 (800) 555-1234 / 0044 20 7946 0958
    public const string PhoneInternational = @"^\+?(?:[0-9\s.\-\(\)]){6,20}$";

    // Strong password: upper, lower, digit, special, min 8
    public const string Password = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d\s]).{8,}$";

    // Username: alphanumeric, underscores, hyphens, 3-20 chars
    public const string Username = @"^[a-zA-Z0-9_\-]{3,20}$";

    // JWT token structure (doesn't validate signature, just shape)
    public const string JwtToken = @"^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]*$";

    // GUID / UUID
    public const string Guid = @"^[{]?[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}[}]?$";

    // US ZIP code (supports ZIP+4)
    public const string ZipCode = @"^\d{5}(-\d{4})?$";

    // UK Postcode
    public const string UkPostcode = @"^[A-Z]{1,2}\d[A-Z\d]?\s*\d[A-Z]{2}$";

    // IPv4
    public const string IPv4 = @"^((25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(25[0-5]|2[0-4]\d|[01]?\d\d?)$";

    // IPv6 (simplified — full validation is brutal in regex)
    public const string IPv6 = @"^([0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}$";

    // MAC address
    public const string MacAddress = @"^([0-9A-Fa-f]{2}[:\-]){5}[0-9A-Fa-f]{2}$";

    // URL (http/https)
    public const string Url = @"^https?:\/\/([\w\-]+\.)+[\w\-]+(\/[\w\-._~:/?#\[\]@!$&'()*+,;=%]*)?$";

    // Slug (URL-friendly string)
    public const string Slug = @"^[a-z0-9]+(?:-[a-z0-9]+)*$";

    // Domain name only
    public const string Domain = @"^(?!-)[a-zA-Z0-9\-]{1,63}(?<!-)(\.[a-zA-Z]{2,})+$";

    // Relative path (no domain)
    public const string RelativePath = @"^(\/[a-zA-Z0-9_\-\.]+)+\/?$";

    // Credit card (major networks, strips spaces/dashes)
    public const string CreditCard = @"^(?:4\d{12}(?:\d{3})?|5[1-5]\d{14}|3[47]\d{13}|6(?:011|5\d{2})\d{12})$";

    // Currency (supports negative, commas, 2 decimal places)
    public const string Currency = @"^-?(?:\d+|\d{1,3}(?:,\d{3})*)(?:\.\d{2})?$";

    // IBAN (basic structure check — not country-specific length)
    public const string Iban = @"^[A-Z]{2}\d{2}[A-Z0-9]{4,30}$";

    // US routing number (9 digits)
    public const string RoutingNumber = @"^\d{9}$";

    // ISO 8601 date
    public const string DateIso = @"^\d{4}-(0[1-9]|1[0-2])-(0[1-9]|[12]\d|3[01])$";

    // US date MM/DD/YYYY
    public const string DateUs = @"^(0[1-9]|1[0-2])\/(0[1-9]|[12]\d|3[01])\/\d{4}$";

    // Time HH:MM (24hr)
    public const string Time24 = @"^([01]\d|2[0-3]):[0-5]\d$";

    // Time with seconds
    public const string Time24WithSeconds = @"^([01]\d|2[0-3]):[0-5]\d:[0-5]\d$";

    // Detects basic HTML tags (for stripping/escaping)
    public const string HtmlTag = @"<[^>]*>";

    // SQL injection red flags (use parameterized queries — this is a last resort)
    public const string SqlInjection =
        @"('(''|[^'])*')|(;)|(\b(SELECT|INSERT|UPDATE|DELETE|DROP|UNION|EXEC|CAST|CONVERT)\b)";

    // No special characters (plain alphanumeric + spaces)
    public const string Alphanumeric = @"^[a-zA-Z0-9\s]*$";

    // Hex color code
    public const string HexColor = @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$";

    // Base64
    public const string Base64 = @"^[A-Za-z0-9+/]*={0,2}$";

    // US SSN (accepts 123-45-6789 or 123456789)
    public const string Ssn = @"^\d{3}-?\d{2}-?\d{4}$";

    // US EIN (employer tax ID)
    public const string Ein = @"^\d{2}-?\d{7}$";

    // ISBN-13
    public const string Isbn13 = @"^97[89]\d{10}$";

    // Semantic version (1.0.0, 2.3.1-beta)
    public const string SemVer = @"^\d+\.\d+\.\d+(-[a-zA-Z0-9.]+)?(\+[a-zA-Z0-9.]+)?$";
}