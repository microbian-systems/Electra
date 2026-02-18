# Plugs Feature Documentation

## Overview

The Plugs feature allows providers to define automated post-processing hooks that run on a schedule. These hooks can perform actions like auto-reposting when a post reaches a certain number of likes, or adding promotional comments.

## Plug Types

### PlugAttribute

General-purpose plug that can run on any schedule. Use for periodic tasks not tied to specific posts.

```csharp
[Plug(
    identifier: "my-plug",
    title: "My Plug",
    description: "Does something periodically",
    runEveryMilliseconds: 3600000, // 1 hour
    totalRuns: 0)] // 0 = unlimited
public async Task MyPlugMethod(
    string integrationId,
    string accessToken,
    CancellationToken cancellationToken = default)
{
    // Your logic here
}
```

### PostPlugAttribute

Post-specific plug that monitors individual posts and triggers based on engagement metrics.

```csharp
[PostPlug(
    identifier: "auto-repost",
    title: "Auto Repost",
    description: "Reposts when a post reaches X likes",
    runEveryMilliseconds: 21600000, // 6 hours
    totalRuns: 10)]
[PlugField("minLikes", "number", "100", "Minimum likes to trigger")]
public async Task AutoRepostPlug(
    string postId,
    string accessToken,
    int minLikes,
    CancellationToken cancellationToken = default)
{
    // Your logic here
}
```

## Configuration Fields

Use `[PlugField]` attribute to define configurable parameters:

```csharp
[PlugField("fieldName", "fieldType", "defaultValue", "Description")]
```

### Supported Field Types

| Type      | Description   | Example         |
| --------- | ------------- | --------------- |
| `string`  | Text input    | `"Hello World"` |
| `number`  | Integer value | `"42"`          |
| `boolean` | True/False    | `"true"`        |

## Validation

Apply validation attributes to fields:

```csharp
[PlugField("minLikes", "number", "10", "Minimum likes")]
[RangeValidation(1, 1000, "Must be between 1 and 1000")]
[RequiredValidation("This field is required")]
```

### Built-in Validations

- `RequiredValidation` - Field must have a value
- `MinValueValidation` - Minimum numeric value
- `MaxValueValidation` - Maximum numeric value
- `RangeValidation` - Value must be within range
- `PatternValidation` - Must match regex pattern

## Usage Examples

### Discovering Plugs

```csharp
var provider = serviceProvider.GetRequiredService<LinkedInPageProvider>();
var plugs = provider.DiscoverPlugs();

foreach (var plug in plugs)
{
    Console.WriteLine($"Found plug: {plug.Title}");
    Console.WriteLine($"  ID: {plug.Identifier}");
    Console.WriteLine($"  Description: {plug.Description}");
}
```

### Executing a Plug

```csharp
var executor = new PlugExecutor(logger);
var plug = provider.GetPlug("auto-repost-post");

var context = new PlugExecutionContext
{
    IntegrationId = "integration-123",
    AccessToken = "access-token",
    PostId = "post-456",
    ScheduledTime = DateTime.UtcNow
};

var fieldValues = new Dictionary<string, object>
{
    ["minLikes"] = 100,
    ["message"] = "Check out this popular post!"
};

var result = await provider.ExecutePlugAsync(
    plug, executor, context, fieldValues);

if (result.Success)
{
    Console.WriteLine("Plug executed successfully!");
}
else
{
    Console.WriteLine($"Plug failed: {result.ErrorMessage}");
}
```

### Scheduling Checks

```csharp
// Check if a plug should run
var shouldRun = executor.ShouldExecute(
    plug.PostPlugAttribute,
    lastRunTime: DateTime.UtcNow.AddHours(-7),
    executionCount: 3);

if (shouldRun)
{
    // Execute the plug
    var result = await provider.ExecutePlugAsync(...);
}
```

## Creating Custom Plugs

1. Create a method in your provider class
2. Add `[PostPlug]` or `[Plug]` attribute
3. Define configuration fields with `[PlugField]`
4. Implement your logic

Example:

```csharp
public class MyProvider : SocialProviderBase
{
    [PostPlug(
        identifier: "custom-action",
        title: "Custom Action",
        description: "Performs a custom action on posts",
        runEveryMilliseconds: 86400000)] // 24 hours
    [PlugField("threshold", "number", "50", "Action threshold")]
    public async Task CustomAction(
        string postId,
        string accessToken,
        int threshold,
        CancellationToken cancellationToken = default)
    {
        // Get post metrics
        var analytics = await PostAnalyticsAsync(...);

        // Check condition
        if (analytics?.FirstOrDefault()?.Data.Count > threshold)
        {
            // Perform action
            await SomeActionAsync(...);
        }
    }
}
```

## Integration with DI

Register the plug executor in your DI container:

```csharp
services.AddSingleton<IPlugExecutor, PlugExecutor>();
```

## Error Handling

Plugs integrate with the provider's existing error handling:

- Validation errors return `PlugExecutionResult.FailedResult()`
- Exceptions are caught and logged
- Failed plugs can be rescheduled based on `ShouldReschedule` property

## Best Practices

1. **Idempotency**: Plugs may run multiple times, ensure they're idempotent
2. **Rate Limiting**: Respect API rate limits in your plug logic
3. **Logging**: Use the provider's Logger for consistent logging
4. **Validation**: Always validate inputs using the validation framework
5. **Scheduling**: Choose appropriate intervals (6+ hours recommended)
6. **Total Runs**: Set reasonable limits to prevent infinite execution

## See Also

- `IPlugExecutor` - Interface for plug execution
- `PlugExecutor` - Default implementation
- `SocialProviderBase.DiscoverPlugs()` - Method to find all plugs in a provider
- `SocialProviderBase.ExecutePlugAsync()` - Method to execute a plug
