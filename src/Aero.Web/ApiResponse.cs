namespace Aero.Common.Web;


public interface IApiResponse
{
    HttpStatusCode StatusCode { get; set; }
    string? Message { get; set; }
}

public interface IApiAuthResponse<T> : IApiResponse
{
    T Data { get; set; }
}

public class ApiAuthResponse : IApiResponse
{
    [JsonPropertyName("statusCode")]
    public HttpStatusCode StatusCode { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class ApiAuthResponse<T> : ApiAuthResponse, IApiAuthResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; }
}