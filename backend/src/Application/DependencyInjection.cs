using Application.CQRS;
using Application.CQRS.Auth.Commands;
using Application.CQRS.Auth.Queries;
using Application.CQRS.Coaches.Commands;
using Application.CQRS.Coaches.Queries;
using Application.CQRS.Matches.Commands;
using Application.CQRS.Matches.Queries;
using Application.CQRS.Notifications.Commands;
using Application.CQRS.Notifications.Queries;
using Application.CQRS.Players.Commands;
using Application.CQRS.Players.Queries;
using Application.CQRS.Seasons.Commands;
using Application.CQRS.Seasons.Queries;
using Application.CQRS.Stadiums.Commands;
using Application.CQRS.Stadiums.Queries;
using Application.CQRS.Teams.Commands;
using Application.CQRS.Teams.Queries;
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
        var assembly = typeof(DependencyInjection).Assembly;
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

        // Auth handlers
        services.AddTransient<IRequestHandler<RegisterUserCommand, RegisterUserCommandResponse>, RegisterUserCommandHandler>();
        services.AddTransient<IRequestHandler<LoginUserCommand, LoginUserCommandResponse>, LoginUserCommandHandler>();
        services.AddTransient<IRequestHandler<RefreshTokenCommand, RefreshTokenCommandResponse>, RefreshTokenCommandHandler>();
        services.AddTransient<IRequestHandler<ForgotPasswordCommand, ForgotPasswordCommandResponse>, ForgotPasswordCommandHandler>();
        services.AddTransient<IRequestHandler<ResetPasswordCommand, ResetPasswordCommandResponse>, ResetPasswordCommandHandler>();
        services.AddTransient<IRequestHandler<ConfirmEmailCommand, ConfirmEmailCommandResponse>, ConfirmEmailCommandHandler>();
        services.AddTransient<IRequestHandler<ResendEmailConfirmationCommand, ResendEmailConfirmationCommandResponse>, ResendEmailConfirmationCommandHandler>();
        services.AddTransient<IRequestHandler<RevokeTokenCommand, RevokeTokenCommandResponse>, RevokeTokenCommandHandler>();
        services.AddTransient<IRequestHandler<UpdateUserCommand, UpdateUserCommandResponse>, UpdateUserCommandHandler>();
        services.AddTransient<IRequestHandler<GetUserProfileQuery, GetUserProfileQueryResponse>, GetUserProfileQueryHandler>();

        // Coach handlers
        services.AddTransient<IRequestHandler<CreateCoachCommand, CreateCoachCommandResponse>, CreateCoachCommandHandler>();
        services.AddTransient<IRequestHandler<UpdateCoachCommand, UpdateCoachCommandResponse>, UpdateCoachCommandHandler>();
        services.AddTransient<IRequestHandler<DeleteCoachCommand, DeleteCoachCommandResponse>, DeleteCoachCommandHandler>();
        services.AddTransient<IRequestHandler<GetAllCoachesQuery, GetAllCoachesQueryResponse>, GetAllCoachesQueryHandler>();
        services.AddTransient<IRequestHandler<GetCoachByIdQuery, GetCoachByIdQueryResponse>, GetCoachByIdQueryHandler>();

        // Match handlers
        services.AddTransient<IRequestHandler<CreateMatchCommand, CreateMatchCommandResponse>, CreateMatchCommandHandler>();
        services.AddTransient<IRequestHandler<UpdateMatchCommand, UpdateMatchCommandResponse>, UpdateMatchCommandHandler>();
        services.AddTransient<IRequestHandler<DeleteMatchCommand, DeleteMatchCommandResponse>, DeleteMatchCommandHandler>();
        services.AddTransient<IRequestHandler<UpdateMatchStatusCommand, UpdateMatchStatusCommandResponse>, UpdateMatchStatusCommandHandler>();
        services.AddTransient<IRequestHandler<GetAllMatchesQuery, GetAllMatchesQueryResponse>, GetAllMatchesQueryHandler>();
        services.AddTransient<IRequestHandler<GetMatchByIdQuery, GetMatchByIdQueryResponse>, GetMatchByIdQueryHandler>();
        services.AddTransient<IRequestHandler<GetMatchByIdWithDetailsQuery, GetMatchByIdWithDetailsQueryResponse>, GetMatchByIdWithDetailsQueryHandler>();
        services.AddTransient<IRequestHandler<GetLiveMatchQuery, GetLiveMatchQueryResponse>, GetLiveMatchQueryHandler>();
        services.AddTransient<IRequestHandler<GetUserMatchesQuery, GetUserMatchesQueryResponse>, GetUserMatchesQueryHandler>();

        // Notification handlers
        services.AddTransient<IRequestHandler<CreateNotificationCommand, CreateNotificationCommandResponse>, CreateNotificationCommandHandler>();
        services.AddTransient<IRequestHandler<GetUserNotificationsQuery, GetUserNotificationsQueryResponse>, GetUserNotificationsQueryHandler>();

        // Player handlers
        services.AddTransient<IRequestHandler<CreatePlayerCommand, CreatePlayerCommandResponse>, CreatePlayerCommandHandler>();
        services.AddTransient<IRequestHandler<UpdatePlayerCommand, UpdatePlayerCommandResponse>, UpdatePlayerCommandHandler>();
        services.AddTransient<IRequestHandler<DeletePlayerCommand, DeletePlayerCommandResponse>, DeletePlayerCommandHandler>();
        services.AddTransient<IRequestHandler<GetAllPlayersQuery, GetAllPlayersQueryResponse>, GetAllPlayersQueryHandler>();
        services.AddTransient<IRequestHandler<GetPlayerByIdQuery, GetPlayerByIdQueryResponse>, GetPlayerByIdQueryHandler>();

        // Season handlers
        services.AddTransient<IRequestHandler<CreateSeasonCommand, CreateSeasonCommandResponse>, CreateSeasonCommandHandler>();
        services.AddTransient<IRequestHandler<UpdateSeasonCommand, UpdateSeasonCommandResponse>, UpdateSeasonCommandHandler>();
        services.AddTransient<IRequestHandler<DeleteSeasonCommand, DeleteSeasonCommandResponse>, DeleteSeasonCommandHandler>();
        services.AddTransient<IRequestHandler<GetAllSeasonsQuery, GetAllSeasonsQueryResponse>, GetAllSeasonsQueryHandler>();
        services.AddTransient<IRequestHandler<GetSeasonByIdQuery, GetSeasonByIdQueryResponse>, GetSeasonByIdQueryHandler>();
        services.AddTransient<IRequestHandler<GetSeasonTeamsQuery, GetSeasonTeamsQueryResponse>, GetSeasonTeamsQueryHandler>();

        // Stadium handlers
        services.AddTransient<IRequestHandler<CreateStadiumCommand, CreateStadiumCommandResponse>, CreateStadiumCommandHandler>();
        services.AddTransient<IRequestHandler<UpdateStadiumCommand, UpdateStadiumCommandResponse>, UpdateStadiumCommandHandler>();
        services.AddTransient<IRequestHandler<DeleteStadiumCommand, DeleteStadiumCommandResponse>, DeleteStadiumCommandHandler>();
        services.AddTransient<IRequestHandler<GetAllStadiumsQuery, GetAllStadiumsQueryResponse>, GetAllStadiumsQueryHandler>();
        services.AddTransient<IRequestHandler<GetStadiumByIdQuery, GetStadiumByIdQueryResponse>, GetStadiumByIdQueryHandler>();

        // Team handlers
        services.AddTransient<IRequestHandler<CreateTeamCommand, CreateTeamCommandResponse>, CreateTeamCommandHandler>();
        services.AddTransient<IRequestHandler<UpdateTeamCommand, UpdateTeamCommandResponse>, UpdateTeamCommandHandler>();
        services.AddTransient<IRequestHandler<DeleteTeamCommand, DeleteTeamCommandResponse>, DeleteTeamCommandHandler>();
        services.AddTransient<IRequestHandler<GetAllTeamsQuery, GetAllTeamsQueryResponse>, GetAllTeamsQueryHandler>();
        services.AddTransient<IRequestHandler<GetTeamByIdQuery, GetTeamByIdQueryResponse>, GetTeamByIdQueryHandler>();
        services.AddTransient<IRequestHandler<GetTeamSeasonsQuery, GetTeamSeasonsQueryResponse>, GetTeamSeasonsQueryHandler>();

        return services;
    }
}
