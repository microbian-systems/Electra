using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Electra.Common.Web.Extensions;

public static class ScalarUIExtensions
{
    public static WebApplication AddScalarUI(this WebApplication app)
    {
        if (!app.Environment.IsProduction())
        {
            app.MapScalarApiReference(options =>
            {
                options
                    .WithPreferredScheme("Bearer")
                    .WithHttpBearerAuthentication(new HttpBearerOptions())
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                    .WithDownloadButton(true)
                    .WithClientButton(true);
            });
        }
        
        return app;
    }
}