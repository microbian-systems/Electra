namespace Aero.Social.Plugs;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public class PlugAttribute : Attribute
{
    public string Identifier { get; }
    public string Title { get; }
    public string Description { get; }
    public int RunEveryMilliseconds { get; }
    public int TotalRuns { get; }
    public List<PlugField> Fields { get; }

    public PlugAttribute(
        string identifier,
        string title,
        string description,
        int runEveryMilliseconds,
        int totalRuns = 0)
    {
        Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        RunEveryMilliseconds = runEveryMilliseconds;
        TotalRuns = totalRuns;
        Fields = new List<PlugField>();
    }

    public PlugAttribute AddField(PlugField field)
    {
        Fields.Add(field);
        return this;
    }
}

public class PlugField
{
    public string Name { get; }
    public string Type { get; }
    public string? Placeholder { get; }
    public string? Description { get; }
    public List<IFieldValidation> Validations { get; }

    public PlugField(
        string name,
        string type,
        string? placeholder = null,
        string? description = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Placeholder = placeholder;
        Description = description;
        Validations = new List<IFieldValidation>();
    }

    public PlugField AddValidation(IFieldValidation validation)
    {
        Validations.Add(validation);
        return this;
    }
}

public interface IFieldValidation
{
    string Type { get; }
    string? ErrorMessage { get; }
    bool Validate(object? value);
}

public class RequiredValidation : IFieldValidation
{
    public string Type => "required";
    public string? ErrorMessage { get; }

    public RequiredValidation(string? errorMessage = null)
    {
        ErrorMessage = errorMessage ?? "This field is required";
    }

    public bool Validate(object? value)
    {
        if (value == null) return false;
        if (value is string str) return !string.IsNullOrWhiteSpace(str);
        return true;
    }
}

public class MinValueValidation : IFieldValidation
{
    public string Type => "min";
    public string? ErrorMessage { get; }
    public int MinValue { get; }

    public MinValueValidation(int minValue, string? errorMessage = null)
    {
        MinValue = minValue;
        ErrorMessage = errorMessage ?? $"Value must be at least {minValue}";
    }

    public bool Validate(object? value)
    {
        if (value == null) return false;
        if (value is int intValue) return intValue >= MinValue;
        if (value is long longValue) return longValue >= MinValue;
        if (value is double doubleValue) return doubleValue >= MinValue;
        if (value is decimal decimalValue) return decimalValue >= MinValue;
        return false;
    }
}

public class MaxValueValidation : IFieldValidation
{
    public string Type => "max";
    public string? ErrorMessage { get; }
    public int MaxValue { get; }

    public MaxValueValidation(int maxValue, string? errorMessage = null)
    {
        MaxValue = maxValue;
        ErrorMessage = errorMessage ?? $"Value must be at most {maxValue}";
    }

    public bool Validate(object? value)
    {
        if (value == null) return false;
        if (value is int intValue) return intValue <= MaxValue;
        if (value is long longValue) return longValue <= MaxValue;
        if (value is double doubleValue) return doubleValue <= MaxValue;
        if (value is decimal decimalValue) return decimalValue <= MaxValue;
        return false;
    }
}

public class RangeValidation : IFieldValidation
{
    public string Type => "range";
    public string? ErrorMessage { get; }
    public int MinValue { get; }
    public int MaxValue { get; }

    public RangeValidation(int minValue, int maxValue, string? errorMessage = null)
    {
        MinValue = minValue;
        MaxValue = maxValue;
        ErrorMessage = errorMessage ?? $"Value must be between {minValue} and {maxValue}";
    }

    public bool Validate(object? value)
    {
        if (value == null) return false;
        if (value is int intValue) return intValue >= MinValue && intValue <= MaxValue;
        if (value is long longValue) return longValue >= MinValue && longValue <= MaxValue;
        if (value is double doubleValue) return doubleValue >= MinValue && doubleValue <= MaxValue;
        if (value is decimal decimalValue) return decimalValue >= MinValue && decimalValue <= MaxValue;
        return false;
    }
}

public class PatternValidation : IFieldValidation
{
    public string Type => "pattern";
    public string? ErrorMessage { get; }
    public string Pattern { get; }

    public PatternValidation(string pattern, string? errorMessage = null)
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        ErrorMessage = errorMessage ?? "Value does not match the required pattern";
    }

    public bool Validate(object? value)
    {
        if (value == null) return false;
        if (value is not string str) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(str, Pattern);
    }
}
