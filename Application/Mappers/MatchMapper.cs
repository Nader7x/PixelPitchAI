using Application.CQRS.Matches.Commands;
using Application.CQRS.Matches.Queries;
using Application.Dtos;
using Domain.Models;
using Riok.Mapperly.Abstractions;

namespace Application.Mappers;

[Mapper(UseDeepCloning = true)] // Added UseDeepCloning for potentially complex object graphs
public partial class MatchMapper
{
    // Map from Match to MatchDto
    public partial MatchDto ToDto(Match match);

    // Map from list of Match to list of MatchDto
    [MapProperty(nameof(Match.HomeTeamInMatchName), nameof(MatchDto.HomeTeamName))]
    [MapProperty(nameof(Match.AwayTeamInMatchName), nameof(MatchDto.AwayTeamName))]
    public partial List<MatchDto?> ToDtoList(IEnumerable<Match> matches);

    // Map from CreateMatchDto to CreateMatchCommand
    public partial CreateMatchCommand ToCreateCommand(CreateMatchDto dto);

    // Map from UpdateMatchDto to UpdateMatchCommand
    public partial UpdateMatchCommand ToUpdateCommand(UpdateMatchDto dto);

    // Map from GetAllMatchesQuery parameters (assuming these are likely filters)
    public partial GetAllMatchesQuery ToGetAllQuery(int? seasonId, int? teamId, DateTime? fromDate, DateTime? toDate);

    // Map from GetMatchByIdQuery parameter
    public partial GetMatchByIdQuery ToGetByIdQuery(int id);

    // Map for Delete command
    public partial DeleteMatchCommand ToDeleteCommand(int id);

    public partial Match ToMatchFromCreate(CreateMatchCommand request);

    // Ensure this mapping correctly handles the new structure of MatchDetailsDto
    // Mapperly will attempt to map properties by name.
    // For example, Match.Season (type Season) to MatchDetailsDto.Season (type SeasonDto)
    // will require a mapping from Season to SeasonDto. This should be defined
    // in a SeasonMapper or handled by Mapperly if the structures are compatible.
    // The same applies to Team, Stadium, Coach.
    // If SeasonMapper, TeamMapper, etc. are in the same assembly and also marked with [Mapper],
    // Mapperly should be able to use them.
    [MapProperty(nameof(Match.HomeTeamSeason), nameof(MatchDetailsDto.HomeTeamSeason))]
    [MapProperty(nameof(Match.AwayTeamSeason), nameof(MatchDetailsDto.AwayTeamSeason))]
    [MapProperty(nameof(Match.HomeTeam), nameof(MatchDetailsDto.HomeTeam))]
    [MapProperty(nameof(Match.AwayTeam), nameof(MatchDetailsDto.AwayTeam))]
    [MapProperty(nameof(Match.Stadium), nameof(MatchDetailsDto.Stadium))]
    [MapProperty(nameof(Match.HomeCoach), nameof(MatchDetailsDto.HomeCoach))]
    [MapProperty(nameof(Match.AwayCoach), nameof(MatchDetailsDto.AwayCoach))]
    public partial MatchDetailsDto ToDetailsFromMatch(Match match);
    [MapProperty(nameof(Match.HomeTeam.Name),nameof(UserMatchDto.HomeTeamName))]
    [MapProperty(nameof(Match.AwayTeam.Name),nameof(UserMatchDto.AwayTeamName))]
    [MapProperty(nameof(Match.HomeTeam.Logo),nameof(UserMatchDto.HomeTeamLogo))]
    [MapProperty(nameof(Match.AwayTeam.Logo),nameof(UserMatchDto.AwayTeamLogo))]
    public partial List<UserMatchDto> ToUserMatchDto(IEnumerable<Match> matches);
}