namespace Electra.Common.Web.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerEx(this IServiceCollection services, IWebHostEnvironment env)
    {
        if (env.IsProduction()) return services;

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }

    public static IApplicationBuilder UseSwaggerEx(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsProduction()) return app;

        app.UseSwagger();
        app.UseSwaggerUI();

        return app;
    }
}