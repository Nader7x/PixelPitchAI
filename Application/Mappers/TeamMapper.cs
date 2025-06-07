using Application.CQRS.Teams.Commands;
using Application.CQRS.Teams.Queries;
using Application.Dtos;
using Domain.Models;
using Riok.Mapperly.Abstractions;

namespace Application.Mappers;

[Mapper]
public partial class TeamMapper
{
    // Map from list of Team to list of TeamDto
    public partial List<TeamDto> ToDtoList(IEnumerable<Team> teams);

    // Map from CreateTeamDto to CreateTeamCommand
    public partial CreateTeamCommand ToCreateCommand(CreateTeamDto dto);

    // Map from UpdateTeamDto to UpdateTeamCommand
    public partial UpdateTeamCommand ToUpdateCommand(UpdateTeamDto dto);

    // Map from GetAllTeamsQuery parameters (assuming these are the likely filters)
    public partial GetAllTeamsQuery ToGetAllQuery(string country, string leagueName);

    // Map from GetTeamByIdQuery parameter
    public partial GetTeamByIdQuery ToGetByIdQuery(int id);

    // Map for Delete command
    public partial DeleteTeamCommand ToDeleteCommand(int id);

    // map from createcommand to a team 
    public partial Team ToTeamfromCreate(CreateTeamCommand command);
    public partial TeamDto ToTeamDto(Team team);
}