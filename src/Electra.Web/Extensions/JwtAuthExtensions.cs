using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Electra.Common.Web.Controllers;
using Electra.Common.Web.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ThrowGuard;

namespace Electra.Common.Web.Extensions;

public static class JwtAuthExtensions
{
    public static IServiceCollection AddJwtAuthorization(
        this IServiceCollection services, 
        IConfiguration config)
    {
        
        var jwtOptions = new JwtOptions();
        config.GetSection("jwt").Bind(jwtOptions);
        services.Configure<JwtOptions>(config.GetSection("jwt"));

        if(string.IsNullOrEmpty(jwtOptions.Key))
            Throw.AppException("JwtOptions is null or JwtOptions.Key is null or empty");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));
        var issuer = jwtOptions.Issuer;
        var audience = jwtOptions.Audience;
        
        services.Configure<JsonOptions>(options =>
        {
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.AllowTrailingCommas = true;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            // options.SerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        });
        
        services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.AllowTrailingCommas = true;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                //options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
            })
            //.AddApplicationPart(typeof(JwtAuthController).Assembly)
            ;

        // todo - disabling jwt until front-end team is ready for jwt
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };
        });

        return services;
    }
    
    public static WebApplicationBuilder AddJwtAuthorization(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;

        builder.Services.AddJwtAuthorization(config);
        
        return builder;
    }
}