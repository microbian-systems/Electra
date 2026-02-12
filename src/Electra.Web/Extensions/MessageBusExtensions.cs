using Microsoft.Extensions.DependencyInjection;

namespace Electra.Common.Web.Extensions;

public static class MessageBusExtensions
{
    public static IServiceCollection AddMessageQueing(this IServiceCollection services)
    {
        //todo - implement method to add MessageQueuing to Electra pipeline

        return services;
    }
}