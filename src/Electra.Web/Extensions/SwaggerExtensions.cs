using System.IO;
using System.Reflection;
using Microsoft.OpenApi.Models;

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

        public static WebApplicationBuilder AddSwaggerEndpoint(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        builder.Services.AddSwaggerEndpoint(config);

        return builder;
    }

    public static IServiceCollection AddSwaggerEndpoint(this IServiceCollection services,
        IConfiguration config)
    {
        // todo -  pull swagger app details from asppsettings (IConfiguration)
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.UseAllOfToExtendReferenceSchemas();
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Cherry Cars API",
                Description = "Allowing interactions with the Dealer Back End",
            });
            //options.SchemaFilter<EnumSchemaFilter>();
            var xmlFilename = $"{Assembly.GetEntryAssembly()?.GetName().Name}.xml";
            try
            {
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            }catch (Exception ex) { /* can be ignored - mainly used in integration test scenarios */ }

            // todo - enable once front-end team is ready to use JWT
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
             {
                 {
                     new OpenApiSecurityScheme
                     {
                         Reference = new OpenApiReference
                         {
                             Type=ReferenceType.SecurityScheme,
                             Id="Bearer"
                         }
                     },
                     Array.Empty<string>()
                 }
             });
        });

        return services;
    }
}