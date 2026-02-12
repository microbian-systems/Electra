using Electra.Core.Identity;
using Electra.Models.Entities;
using Electra.Persistence;
using Electra.Services;
using Microsoft.AspNetCore.Identity;

namespace Electra.Common.Web.Extensions;

public static class AspNetIdentityExtensions
{
    public static IServiceCollection AddAspNetIdentityEx(this IServiceCollection services,
        IConfiguration config, IWebHostEnvironment env)
    {


        services.AddScoped<IElectraIdentityService, ElectraIdentityService>();
        services.AddScoped<IElectraUserProfileService, ElectraUserProfileService>();

        return services;
    }
}