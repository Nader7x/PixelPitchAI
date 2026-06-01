using Application.CQRS.Teams.Commands;
using Application.CQRS.Teams.Queries;
using Application.Dtos;
using Domain.Models;

namespace Application.Interfaces;

public interface ITeamMapper
{
    List<TeamDto> ToDtoList(IEnumerable<Team> teams);
    CreateTeamCommand ToCreateCommand(CreateTeamDto dto);
    UpdateTeamCommand ToUpdateCommand(UpdateTeamDto dto);
    GetAllTeamsQuery ToGetAllQuery(string country, string leagueName);
    GetTeamByIdQuery ToGetByIdQuery(int id);
    DeleteTeamCommand ToDeleteCommand(int id);
    Team ToTeamfromCreate(CreateTeamCommand command);
    TeamDto ToTeamDto(Team team);
    void UpdateTeamFromCommand(UpdateTeamCommand command, Team team);
}
