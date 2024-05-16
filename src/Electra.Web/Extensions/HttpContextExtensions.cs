using System.IO;
using System.Text;
using Electra.Models;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Electra.Common.Web.Extensions;

public static class HttpContextExtensions
{
    public static async Task<RequestResponseLogModel> ToRequestResponseLogModel(this ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;
        request.EnableBuffering();

        await using var stream = request.BodyReader.AsStream(true);
        var body = await new StreamReader(stream).ReadToEndAsync();
        context.HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);

        var model = context.HttpContext.ToRequestResponseLogModel();

        return model with {RequestBody = body};
    }
    
    public static async Task<RequestResponseLogModel> ToRequestResponseLogModel(this ActionExecutedContext context)
    {
        var response = context.HttpContext.Response;
        var model = context.HttpContext.ToRequestResponseLogModel();

        if (response.Body is not { CanRead: true, CanSeek: true }) 
            return model;
        
        // todo - figure out how to read the body here (could be several object types)
        context.HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(response.Body, leaveOpen: true).ReadToEndAsync();
        context.HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);
        
        return model with { ResponseBody = body };

    }
    
    public static RequestResponseLogModel ToRequestResponseLogModel(this HttpContext? context)
    {
        if (context is null)
            return new();
        
        var model = new RequestResponseLogModel()
        {
            Scheme = context.Request.Scheme,
            RequestBody = "omitted",
            ResponseBody = "omitted",
            Host = context.Request.Host.ToString(),
            RequestHeaders = context.Request.Headers?.ToDictionary(x => x.Key, x => x.Value.ToString())!,
            Method = context.Request.Method,
            Path = context.Request.Path,
            QueryString = context.Request.QueryString.ToString(),
            ResponseHeaders = context.Response.Headers?.ToDictionary(x => x.Key, x => x.Value.ToString())!,
            ResponseStatusCode = context.Response.StatusCode,
            ResponseStatusDescription = context.Response.StatusCode.ToString(), // todo - get reasonphrase if possible
            ResponseContentType = context.Response.ContentType,
            RequestProtocol = context.Request.Protocol,
            RequestRemoteIpAddress = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            RequestRemotePort = context.Connection.RemotePort.ToString(),
            RequestLocalIpAddress = context.Connection.LocalIpAddress?.ToString() ?? string.Empty,
            RequestLocalPort = context.Connection.LocalPort.ToString(),
            RequestId = context.TraceIdentifier,
            RequestTraceIdentifier = context.TraceIdentifier,
            RequestIsHttps = context.Request.IsHttps,
            RequestIsWebSocketRequest = context.WebSockets.IsWebSocketRequest,
            RequestIsSecureConnection = context.Request.IsHttps,
            RequestIsLocal = context.Request.IsLocal(),
            RequestIsAuthenticated = context.User?.Identity?.IsAuthenticated ?? false,
        };

        return model;
    }
    
    public static bool IsLocal(this HttpRequest req)
    {
        var connection = req.HttpContext.Connection;
        if (connection.RemoteIpAddress != null && connection.RemoteIpAddress.IsSet())
        {
            //We have a remote address set up
            return connection.LocalIpAddress!.IsSet() 
                //Is local is same as remote, then we are local
                ? connection.RemoteIpAddress.Equals(connection.LocalIpAddress) 
                //else we are remote if the remote IP address is not a loopback address
                : IPAddress.IsLoopback(connection.RemoteIpAddress);
        }

        return true;
    }

    private static bool IsSet(this IPAddress address)
    {
        const string nullIpAddress = "::1";
        return address != null && address.ToString() != nullIpAddress;
    }

    public static Tuple<string,string> GetUsedPassFromBasicAuth(this HttpRequest req)
    {
        try
        {
            string basic = req.Headers.Authorization.ToString() ?? "";
            basic = basic.Replace("Basic ", "");
            var encoding = Encoding.GetEncoding("iso-8859-1");
            basic = encoding.GetString(Convert.FromBase64String(basic));
            int separator = basic.IndexOf(':');
            return new Tuple<string, string>(basic.Substring(0, separator), basic.Substring(separator + 1));
        }
        catch {
            return new Tuple<string, string>("", "");
        }

    }

    public static async Task<string> GetBodyOfRequest(this HttpRequest req)
    {
        try
        {
            req.Body.Seek(0, SeekOrigin.Begin);
            await using var stream = req.BodyReader.AsStream(true);
            var body = await new StreamReader(stream).ReadToEndAsync();
            return body;
        }
        catch(Exception ex) 
        {
            return ex.Message; 
        }

    }
}