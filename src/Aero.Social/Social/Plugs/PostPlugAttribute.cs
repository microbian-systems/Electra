namespace Aero.Social.Plugs;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public class PostPlugAttribute : Attribute
{
    public string Identifier { get; }
    public string Title { get; }
    public string Description { get; }
    public int RunEveryMilliseconds { get; }
    public int TotalRuns { get; }
    public List<PlugField> Fields { get; }
    
    /// <summary>
    /// Specifies the trigger condition for the plug (e.g., "likes", "comments", "shares")
    /// </summary>
    public string? TriggerOn { get; set; }
    
    /// <summary>
    /// The threshold value for the trigger (e.g., number of likes needed)
    /// </summary>
    public int TriggerThreshold { get; set; }

    public PostPlugAttribute(
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

    public PostPlugAttribute AddField(PlugField field)
    {
        Fields.Add(field);
        return this;
    }
}

/// <summary>
/// Attribute to define a field for a plug. Can be applied multiple times to a method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public class PlugFieldAttribute : Attribute
{
    public string Name { get; }
    public string Type { get; }
    public string? Placeholder { get; }
    public string? Description { get; }

    public PlugFieldAttribute(
        string name,
        string type,
        string? placeholder = null,
        string? description = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Placeholder = placeholder;
        Description = description;
    }
}
