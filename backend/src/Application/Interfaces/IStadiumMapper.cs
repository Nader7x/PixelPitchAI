using Application.CQRS.Stadiums.Commands;
using Application.CQRS.Stadiums.Queries;
using Application.Dtos;
using Domain.Models;

namespace Application.Interfaces;

public interface IStadiumMapper
{
    StadiumDto ToDto(Stadium stadium);
    List<StadiumDto> ToDtoList(IEnumerable<Stadium> stadiums);
    CreateStadiumCommand ToCreateCommand(CreateStadiumDto dto);
    UpdateStadiumCommand ToUpdateCommand(UpdateStadiumDto dto);
    GetAllStadiumsQuery ToGetAllQuery(string country, string? city);
    CreateStadiumCommand FromCreatedtoTocommand(CreateStadiumDto dto);
    GetStadiumByIdQuery ToGetByIdQuery(int id);
    DeleteStadiumCommand ToDeleteCommand(int id);
    Stadium ToStadiumFromCreate(CreateStadiumCommand request);
    void UpdateStadiumFromCommand(UpdateStadiumCommand command, Stadium stadium);
}
