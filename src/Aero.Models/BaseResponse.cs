namespace Aero.Models;


// todo - consider replacing webresponse with fluentresults or DotNext Result<> class
public class WebResponse : BaseResponse<object>
{
}

public class WebResponse<T> : BaseResponse<T>
{
}

public abstract class BaseResponse<T>
{
    /// <summary>
    /// non-error response message
    /// </summary>
    /// <example>The club profile was successfully created</example>
    [JsonPropertyName("message")]
    public string Message { get; set; }

    /// <summary>
    /// Data/payload to return
    /// </summary>
    [JsonPropertyName("data")]
    public T Data { get; set; }

    /// <summary>
    /// List of errors
    /// </summary>
    [JsonPropertyName("errors")]
    public List<BaseErrorResponse> Errors { get; } = new();
}