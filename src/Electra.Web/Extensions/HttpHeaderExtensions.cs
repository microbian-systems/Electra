namespace Electra.Common.Web.Extensions;

public static class HttpHeaderExtensions
{
    public static WebApplicationBuilder RemoveHeaders(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

        return builder;
    }
}