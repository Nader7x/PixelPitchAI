using Application.Interfaces;
using Application.Mappers;
using Application.Services;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Add your application services here
        // Example: services.AddTransient<IMyService, MyService>();
        var assembly = typeof(DependencyInjection).Assembly;
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<ApplicationAssemblyReference>();
        });
        services.AddValidatorsFromAssembly(assembly);

        // Note: ITokenService is now registered in Infrastructure
        services.AddScoped<ITokenService, TokenService>();

        // Register mapper interfaces with their implementations
        services.AddSingleton<ISeasonMapper, SeasonMapper>();
        services.AddSingleton<ITeamMapper, TeamMapper>();
        services.AddSingleton<IMatchMapper, MatchMapper>();
        services.AddSingleton<IStadiumMapper, StadiumMapper>();
        services.AddSingleton<IUserMapper, UserMapper>();
        services.AddSingleton<IPlayerMapper, PlayerMapper>();
        services.AddSingleton<ICoachMapper, CoachMapper>();

        services.AddScoped<IFileStorageService, AzureBlobStorageService>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<MatchHub>();
        services.AddHttpClient();

        return services;
    }
}
