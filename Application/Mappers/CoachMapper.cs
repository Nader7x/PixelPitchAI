using Application.CQRS.Coaches.Commands;
using Application.CQRS.Coaches.Queries;
using Application.Dtos;
using Application.Interfaces;
using Domain.Models;
using Riok.Mapperly.Abstractions;

namespace Application.Mappers;

[Mapper]
public partial class CoachMapper : ICoachMapper
{
    // Map from Coach to CoachDto
    public partial CoachDto ToDto(Coach coach);

    // Map from list of Coach to list of CoachDto
    public partial List<CoachDto> ToDtoList(IEnumerable<Coach> coaches);

    // Map from CreateCoachDto to CreateCoachCommand
    public partial CreateCoachCommand ToCreateCommand(CreateCoachDto dto);

    // Map from UpdateCoachDto to UpdateCoachCommand
    public partial UpdateCoachCommand ToUpdateCommand(UpdateCoachDto dto);

    // Map from GetAllCoachesQuery parameters
    public partial GetAllCoachesQuery ToGetAllQuery(string nationality, int? teamId);

    // Map from GetCoachByIdQuery parameter
    public partial GetCoachByIdQuery ToGetByIdQuery(int id);

    // Map for Delete command
    public partial DeleteCoachCommand ToDeleteCommand(int id);

    public partial Coach ToCoachFromCreate(CreateCoachCommand request);
}