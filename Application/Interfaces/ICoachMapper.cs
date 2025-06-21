using Application.CQRS.Coaches.Commands;
using Application.CQRS.Coaches.Queries;
using Application.Dtos;
using Domain.Models;

namespace Application.Interfaces;

public interface ICoachMapper
{
    CoachDto ToDto(Coach coach);
    List<CoachDto> ToDtoList(IEnumerable<Coach> coaches);
    CreateCoachCommand ToCreateCommand(CreateCoachDto dto);
    UpdateCoachCommand ToUpdateCommand(UpdateCoachDto dto);
    GetCoachByIdQuery ToGetByIdQuery(int id);
    DeleteCoachCommand ToDeleteCommand(int id);
    Coach ToCoachFromCreate(CreateCoachCommand command);
}
