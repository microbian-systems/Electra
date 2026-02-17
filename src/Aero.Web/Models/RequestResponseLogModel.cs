namespace Aero.Common.Web.Models;

public record RequestResponseLogModel()
{
    public string Scheme { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string QueryString { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string RequestBody { get; set; } = string.Empty;
    public string ResponseBody { get; set; } = string.Empty;
    public string ResponseContentType { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public string ResponseStatusMessage { get; set; } = string.Empty;
    public string ResponseStatusDescription { get; set; } = string.Empty;
    public Dictionary<string, string> ResponseHeaders { get; set; } = new();
    public Dictionary<string, string> RequestHeaders { get; set; } = new();
    public string RequestProtocol { get; set; } = string.Empty;
    public string RequestRemoteIpAddress { get; set; } = string.Empty;
    public string RequestRemotePort { get; set; } = string.Empty;
    public string RequestLocalIpAddress { get; set; } = string.Empty;
    public string RequestLocalPort { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string RequestTraceIdentifier { get; set; } = string.Empty;
    public bool RequestIsHttps { get; set; }
    public bool RequestIsWebSocketRequest { get; set; }
    public bool RequestIsSecureConnection { get; set; }
    public bool RequestIsLocal { get; set; }
    public bool RequestIsAuthenticated { get; set; }
}