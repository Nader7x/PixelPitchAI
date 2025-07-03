using Application.CQRS.Stadiums.Commands;
using Application.CQRS.Stadiums.Queries;
using Application.Dtos;
using Application.Interfaces;
using Domain.Models;
using Riok.Mapperly.Abstractions;

namespace Application.Mappers;

[Mapper]
public partial class StadiumMapper : IStadiumMapper
{
    // Map from Stadium to StadiumDto
    public partial StadiumDto ToDto(Stadium stadium);

    // Map from list of Stadium to list of StadiumDto
    public partial List<StadiumDto> ToDtoList(IEnumerable<Stadium> stadiums);

    // Map from CreateStadiumDto to CreateStadiumCommand
    public partial CreateStadiumCommand ToCreateCommand(CreateStadiumDto dto);

    // Map from UpdateStadiumDto to UpdateStadiumCommand
    public partial UpdateStadiumCommand ToUpdateCommand(UpdateStadiumDto dto);

    // Map from GetAllStadiumsQuery parameters
    public partial GetAllStadiumsQuery ToGetAllQuery(string country, string? city);

    public partial CreateStadiumCommand FromCreatedtoTocommand(CreateStadiumDto dto);

    // Map from GetStadiumByIdQuery parameter
    public partial GetStadiumByIdQuery ToGetByIdQuery(int id);

    // Map for Delete command
    public partial DeleteStadiumCommand ToDeleteCommand(int id);

    public partial Stadium ToStadiumFromCreate(CreateStadiumCommand request);
}
