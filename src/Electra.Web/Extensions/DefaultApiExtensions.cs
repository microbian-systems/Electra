using Serilog;
using Serilog.Events;

namespace Electra.Common.Web.Extensions;


// todo - consolidate the default api extensions w/ the electra extensions
public static class DefaultApiExtensions
{
    public static WebApplicationBuilder ConfigureDefaultApi(this WebApplicationBuilder builder)
    {
        builder.AddDefaultLogging();
        builder.RemoveHeaders();
        builder.AddDefaultApiServices();
        builder.Services.AddHealthChecks();
        builder.AddApiKeyGenerator();
        builder.AddJwtAuthorization();
        builder.AddApiAuthDbContext();

        return builder;
    }

    
    /// <inheritdoc cref="UseDefaultApi(Microsoft.AspNetCore.Builder.WebApplicationBuilder,System.Action{Microsoft.AspNetCore.Builder.WebApplication},bool)"/>
    /// <param name="builder"></param>
    /// <param name="configure">Additional configuration capabilities for the Default API configuration. Can be overridden</param>
    /// <param name="overrideDefaults"></param>
    public static WebApplication UseDefaultApi(
        this WebApplicationBuilder builder,
        Action<WebApplication> configure,
        bool overrideDefaults = false)
    {
        var app = overrideDefaults switch
        {
            true => builder.Build(),
            false => builder.UseDefaultApi()
        };
        
        configure.Invoke(app);

        return app;
    }
    
    /// <summary>
    /// Used to register any Use() methods that require async operations (awaited)
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configure">An async caapable lambda to configure the web api to use additional features outside of the defaults</param>
    /// <param name="overrideDefaults">used to skip the default api settings (roll your own)</param>
    /// <returns></returns>
    public static WebApplication UseDefaultApi(
        this WebApplicationBuilder builder,
        Func<WebApplication, Task> configure,
        bool overrideDefaults = false)
    {
        var app = builder.UseDefaultApi(app =>
        {
            configure.Invoke(app);
        }, overrideDefaults);

        return app;
    }
    
    public static WebApplication UseDefaultApi(this WebApplicationBuilder builder)
    {
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseSerilogRequestLogging( opts =>
        {
            // Customize the message template
            //opts.MessageTemplate = "Handled {RequestPath}";
    
            // Emit debug-level events instead of the defaults
            opts.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Debug;
    
            // Attach additional properties to the request completion event
            opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("SerilogReqLog", $"request {httpContext.Request.Path}");
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                // var request = httpContext.Request;
                // request.EnableBuffering();
                // var body = "";
                // try
                // {
                //     request.Body.Position = 0;
                //     using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                //     body = reader.ReadToEndAsync()
                //         .GetAwaiter()
                //         .GetResult();
                //     request.Body.Position = 0;
                // }
                // catch (Exception ex)
                // {
                //     diagnosticContext.Set("RequestBody", ex.Message);
                // }
                // diagnosticContext.Set("RequestBody", body);
            };
        });

        app.UseHttpsRedirection();
        
        app.MapControllers();
        app.UseRouting();
        
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseHealthChecks("/ping");

        return app;
    }
    
    public static WebApplicationBuilder ConfigureDefaultApi(
        this WebApplicationBuilder builder, 
        Action configure, 
        bool overrideDefaults=false)
    {
        configure.Invoke();
        
        if(!overrideDefaults)
            builder.ConfigureDefaultApi();
        
        return builder;
    }
    
    public static WebApplicationBuilder ConfigureDefaultApi(
        this WebApplicationBuilder builder,
        Action<WebApplicationBuilder> configure,
        bool overrideDefaults=false)
    {
        configure.Invoke(builder);
        
        if(!overrideDefaults)
            builder.ConfigureDefaultApi();
        
        return builder;
    }
}