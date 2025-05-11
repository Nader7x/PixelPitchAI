using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Add your infrastructure services here
        // Example: services.AddTransient<IMyService, MyService>();
        return services;
    }
}