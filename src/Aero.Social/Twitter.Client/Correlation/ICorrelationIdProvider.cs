namespace Aero.Social.Twitter.Client.Correlation;

/// <summary>
/// Interface for providing correlation IDs for request tracking.
/// </summary>
public interface ICorrelationIdProvider
{
    /// <summary>
    /// Generates a new correlation ID.
    /// </summary>
    /// <returns>A unique correlation ID string.</returns>
    string GenerateCorrelationId();
}

/// <summary>
/// Default implementation of correlation ID provider using GUIDs.
/// </summary>
public class GuidCorrelationIdProvider : ICorrelationIdProvider
{
    /// <inheritdoc />
    public string GenerateCorrelationId()
    {
        return Guid.NewGuid().ToString("N")[..16]; // 16 chars is enough for uniqueness
    }
}