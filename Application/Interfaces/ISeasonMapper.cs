using Application.CQRS.Seasons.Commands;
using Application.Dtos;
using Domain.Models;

namespace Application.Interfaces;

public interface ISeasonMapper
{
    SeasonDto ToDto(Season season);
    List<SeasonDto> ToDtoList(IEnumerable<Season> seasons);
    CreateSeasonCommand ToCreateCommand(CreateSeasonDto dto);
    UpdateSeasonCommand ToUpdateCommand(UpdateSeasonDto dto);
}
