using Aero.CMS.Core.Data;
using Aero.CMS.Core.Settings;
using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;

namespace Aero.CMS.Core.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddAeroCmsCore(this IServiceCollection services, IConfiguration configuration)
    {
        var ravenDbSettings = configuration.GetSection("Aero:RavenDb").Get<RavenDbSettings>() 
                             ?? new RavenDbSettings();

        services.AddSingleton<IDocumentStore>(_ => DocumentStoreFactory.Create(ravenDbSettings));
        
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<IKeyVaultService, EnvironmentKeyVaultService>();

        return services;
    }
}
