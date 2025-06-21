using Application.CQRS.Matches.Commands;
using Application.CQRS.Matches.Queries;
using Application.Dtos;
using Domain.Models;

namespace Application.Interfaces;

public interface IMatchMapper
{
    MatchDto ToDto(Match match);
    List<MatchDto?> ToDtoList(IEnumerable<Match> matches);
    CreateMatchCommand ToCreateCommand(CreateMatchDto dto);
    UpdateMatchCommand ToUpdateCommand(UpdateMatchDto dto);
    GetAllMatchesQuery ToGetAllQuery(int? seasonId, int? teamId, DateTime? fromDate, DateTime? toDate);
    GetMatchByIdQuery ToGetByIdQuery(int id);
    DeleteMatchCommand ToDeleteCommand(int id);
    Match ToMatchFromCreate(CreateMatchCommand request);
    MatchDetailsDto ToDetailsFromMatch(Match match);
    List<UserMatchDto> ToUserMatchDto(IEnumerable<Match> matches);
}
