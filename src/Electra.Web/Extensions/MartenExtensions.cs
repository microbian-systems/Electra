using Electra.Persistence.Marten;
using JasperFx;
using Marten;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Electra.Common.Web.Extensions;

public static class MartenExtensions
{
    public static IServiceCollection ConfigureMarten(this IServiceCollection services, IConfiguration config, IWebHostEnvironment host)
    {
        var connString = config.GetConnectionString("Postgres");

        var marten = services.AddMarten(opts =>
        {
            opts.Connection(connString);
            if (host.IsDevelopment())
            {
                opts.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
            }
        });

        services.AddScoped<IDynamicMartenRepository, DynamicMartinRepository>();
        services.AddScoped(typeof(IGenericMartenRepository<>), typeof(GenericMartenRepository<>));

        //if (host.IsDevelopment())
        //marten.InitializeStore();
        marten.InitializeWith();

        return services;
    }
}