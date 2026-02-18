namespace Aero.Core.Validation;

using System.Text.RegularExpressions;
using Aero.Common.Constants;

/// <summary>
/// Base fluent rule builder for string fields.
/// Uses CRTP so every method returns <typeparamref name="TBuilder"/>, preserving
/// the concrete type through the chain — no casts needed at the call site.
/// </summary>
/// <typeparam name="TBuilder">The concrete builder type (CRTP self-reference).</typeparam>
public abstract class StringRuleBuilder<TBuilder> : IFieldValidator
    where TBuilder : StringRuleBuilder<TBuilder>
{
    protected readonly string FieldName;
    protected readonly string? Value;
    private readonly List<Func<string?, string, ValidationError?>> _rules = [];

    protected StringRuleBuilder(string fieldName, string? value)
    {
        FieldName = fieldName;
        Value = value;
    }

    // -------------------------------------------------------------------------
    // Presence
    // -------------------------------------------------------------------------

    public TBuilder NotEmpty(string? message = null)
        => AddRule((val, field) => string.IsNullOrWhiteSpace(val)
            ? Fail(field, message ?? $"'{field}' is required.")
            : null);

    public TBuilder MaxLength(int max, string? message = null)
        => AddRule((val, field) => val is not null && val.Length > max
            ? Fail(field, message ?? $"'{field}' must not exceed {max} characters.")
            : null);

    public TBuilder MinLength(int min, string? message = null)
        => AddRule((val, field) => val is not null && val.Length < min
            ? Fail(field, message ?? $"'{field}' must be at least {min} characters.")
            : null);

    // -------------------------------------------------------------------------
    // Identity / Auth
    // -------------------------------------------------------------------------

    public TBuilder MustBeEmail(string? message = null)
        => AddPatternRule(RegExConstants.Email,
            message ?? $"'{FieldName}' must be a valid email address.");

    public TBuilder MustBeValidPassword(string? message = null)
        => AddPatternRule(RegExConstants.Password,
            message ?? $"'{FieldName}' must contain uppercase, lowercase, digit, and special character (min 8 chars).");

    public TBuilder MustBeValidUsername(string? message = null)
        => AddPatternRule(RegExConstants.Username,
            message ?? $"'{FieldName}' must be 3–20 alphanumeric characters, underscores, or hyphens.");

    public TBuilder MustBeValidJwt(string? message = null)
        => AddPatternRule(RegExConstants.JwtToken,
            message ?? $"'{FieldName}' must be a valid JWT token.");

    public TBuilder MustBeValidGuid(string? message = null)
        => AddPatternRule(RegExConstants.Guid,
            message ?? $"'{FieldName}' must be a valid GUID.");

    // -------------------------------------------------------------------------
    // Phone
    // -------------------------------------------------------------------------

    public TBuilder MustBePhone(string? message = null)
        => AddPatternRule(RegExConstants.Phone,
            message ?? $"'{FieldName}' must be a valid US phone number.");

    public TBuilder MustBePhoneE164(string? message = null)
        => AddPatternRule(RegExConstants.PhoneE164,
            message ?? $"'{FieldName}' must be in E.164 format (e.g. +14155552671).");

    public TBuilder MustBeInternationalPhone(string? message = null)
        => AddPatternRule(RegExConstants.PhoneInternational,
            message ?? $"'{FieldName}' must be a valid international phone number.");

    // -------------------------------------------------------------------------
    // Address / Location
    // -------------------------------------------------------------------------

    public TBuilder MustBeZipCode(string? message = null)
        => AddPatternRule(RegExConstants.ZipCode,
            message ?? $"'{FieldName}' must be a valid US ZIP code.");

    public TBuilder MustBeUkPostcode(string? message = null)
        => AddPatternRule(RegExConstants.UkPostcode,
            message ?? $"'{FieldName}' must be a valid UK postcode.");

    public TBuilder MustBeIPv4(string? message = null)
        => AddPatternRule(RegExConstants.IPv4,
            message ?? $"'{FieldName}' must be a valid IPv4 address.");

    public TBuilder MustBeIPv6(string? message = null)
        => AddPatternRule(RegExConstants.IPv6,
            message ?? $"'{FieldName}' must be a valid IPv6 address.");

    public TBuilder MustBeMacAddress(string? message = null)
        => AddPatternRule(RegExConstants.MacAddress,
            message ?? $"'{FieldName}' must be a valid MAC address.");

    // -------------------------------------------------------------------------
    // Web
    // -------------------------------------------------------------------------

    public TBuilder MustBeUrl(string? message = null)
        => AddPatternRule(RegExConstants.Url,
            message ?? $"'{FieldName}' must be a valid URL.");

    public TBuilder MustBeSlug(string? message = null)
        => AddPatternRule(RegExConstants.Slug,
            message ?? $"'{FieldName}' must be a valid URL slug (lowercase, hyphens only).");

    public TBuilder MustBeDomain(string? message = null)
        => AddPatternRule(RegExConstants.Domain,
            message ?? $"'{FieldName}' must be a valid domain name.");

    public TBuilder MustBeRelativePath(string? message = null)
        => AddPatternRule(RegExConstants.RelativePath,
            message ?? $"'{FieldName}' must be a valid relative path.");

    // -------------------------------------------------------------------------
    // Financial
    // -------------------------------------------------------------------------

    public TBuilder MustBeCreditCard(string? message = null)
        => AddPatternRule(RegExConstants.CreditCard,
            message ?? $"'{FieldName}' must be a valid credit card number.");

    public TBuilder MustBeCurrency(string? message = null)
        => AddPatternRule(RegExConstants.Currency,
            message ?? $"'{FieldName}' must be a valid currency value.");

    public TBuilder MustBeIban(string? message = null)
        => AddPatternRule(RegExConstants.Iban,
            message ?? $"'{FieldName}' must be a valid IBAN.");

    public TBuilder MustBeRoutingNumber(string? message = null)
        => AddPatternRule(RegExConstants.RoutingNumber,
            message ?? $"'{FieldName}' must be a valid 9-digit routing number.");

    // -------------------------------------------------------------------------
    // Date / Time
    // -------------------------------------------------------------------------

    public TBuilder MustBeIsoDate(string? message = null)
        => AddPatternRule(RegExConstants.DateIso,
            message ?? $"'{FieldName}' must be a valid ISO 8601 date (YYYY-MM-DD).");

    public TBuilder MustBeUsDate(string? message = null)
        => AddPatternRule(RegExConstants.DateUs,
            message ?? $"'{FieldName}' must be a valid US date (MM/DD/YYYY).");

    public TBuilder MustBeTime24(string? message = null)
        => AddPatternRule(RegExConstants.Time24,
            message ?? $"'{FieldName}' must be a valid 24-hour time (HH:MM).");

    public TBuilder MustBeTime24WithSeconds(string? message = null)
        => AddPatternRule(RegExConstants.Time24WithSeconds,
            message ?? $"'{FieldName}' must be a valid 24-hour time with seconds (HH:MM:SS).");

    // -------------------------------------------------------------------------
    // Content / Sanitization
    // -------------------------------------------------------------------------

    public TBuilder MustBeAlphanumeric(string? message = null)
        => AddPatternRule(RegExConstants.Alphanumeric,
            message ?? $"'{FieldName}' must contain only alphanumeric characters and spaces.");

    public TBuilder MustBeHexColor(string? message = null)
        => AddPatternRule(RegExConstants.HexColor,
            message ?? $"'{FieldName}' must be a valid hex color (e.g. #FF0000 or #FFF).");

    public TBuilder MustBeBase64(string? message = null)
        => AddPatternRule(RegExConstants.Base64,
            message ?? $"'{FieldName}' must be valid Base64 encoded data.");

    /// <summary>Fails if the value contains any HTML tags. Use before persisting rich text.</summary>
    public TBuilder MustNotContainHtml(string? message = null)
        => AddRule((val, field) =>
            val is not null && Regex.IsMatch(val, RegExConstants.HtmlTag)
                ? Fail(field, message ?? $"'{field}' must not contain HTML tags.")
                : null);

    /// <summary>
    /// Fails if the value matches known SQL injection patterns.
    /// This is a last-resort guard — always use parameterized queries or EF Core.
    /// </summary>
    public TBuilder MustNotContainSqlInjection(string? message = null)
        => AddRule((val, field) =>
            val is not null && Regex.IsMatch(val, RegExConstants.SqlInjection, RegexOptions.IgnoreCase)
                ? Fail(field, message ?? $"'{field}' contains potentially unsafe SQL content.")
                : null);

    // -------------------------------------------------------------------------
    // Identifiers
    // -------------------------------------------------------------------------

    public TBuilder MustBeSsn(string? message = null)
        => AddPatternRule(RegExConstants.Ssn,
            message ?? $"'{FieldName}' must be a valid US Social Security Number.");

    public TBuilder MustBeEin(string? message = null)
        => AddPatternRule(RegExConstants.Ein,
            message ?? $"'{FieldName}' must be a valid US EIN (XX-XXXXXXX).");

    public TBuilder MustBeIsbn13(string? message = null)
        => AddPatternRule(RegExConstants.Isbn13,
            message ?? $"'{FieldName}' must be a valid ISBN-13.");

    public TBuilder MustBeSemVer(string? message = null)
        => AddPatternRule(RegExConstants.SemVer,
            message ?? $"'{FieldName}' must be a valid semantic version (e.g. 1.0.0).");

    // -------------------------------------------------------------------------
    // Escape hatches
    // -------------------------------------------------------------------------

    /// <summary>Validates against a custom regex pattern.</summary>
    public TBuilder Matches(string pattern, string? message = null)
        => AddPatternRule(pattern, message ?? $"'{FieldName}' does not match the required format.");

    /// <summary>Validates against a custom predicate.</summary>
    public TBuilder Must(Func<string?, bool> predicate, string message)
        => AddRule((val, _) => predicate(val) ? null : new ValidationError(FieldName, message));

    // -------------------------------------------------------------------------
    // Core
    // -------------------------------------------------------------------------

    /// <summary>
    /// Adds a pattern rule. Null values are skipped — pair with <see cref="NotEmpty"/>
    /// to also reject nulls/empty strings.
    /// </summary>
    private TBuilder AddPatternRule(string pattern, string message, RegexOptions options = RegexOptions.None)
        => AddRule((val, field) =>
            val is not null && !Regex.IsMatch(val, pattern, options)
                ? Fail(field, message)
                : null);

    protected TBuilder AddRule(Func<string?, string, ValidationError?> rule)
    {
        _rules.Add(rule);
        return (TBuilder)this;
    }

    public IEnumerable<ValidationError> GetErrors()
        => _rules.Select(r => r(Value, FieldName)).OfType<ValidationError>();

    private static ValidationError Fail(string field, string message)
        => new(field, message);
}