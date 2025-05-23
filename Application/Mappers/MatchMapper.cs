using Application.CQRS.Matches.Commands;
using Application.CQRS.Matches.Queries;
using Application.Dtos;
using Domain.Models;
using Riok.Mapperly.Abstractions;

namespace Application.Mappers;

[Mapper]
public partial class MatchMapper
{
    // Map from Match to MatchDto
    public partial MatchDto ToDto(Match match);
    
    // Map from list of Match to list of MatchDto
    public partial List<MatchDto> ToDtoList(IEnumerable<Match> matches);
    
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
    public partial MatchDetailsDto ToDetailsFromMatch(Match match);
}
