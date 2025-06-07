using Application.CQRS.Seasons.Commands;
using Application.Dtos;
using Domain.Models;
using Riok.Mapperly.Abstractions;

namespace Application.Mappers;

[Mapper]
public partial class SeasonMapper
{
    // Map from Season to SeasonDto
    public partial SeasonDto ToDto(Season season);

    // Map from list of Season to list of SeasonDto 
    public partial List<SeasonDto> ToDtoList(IEnumerable<Season> seasons);

    // Map from CreateSeasonDto to CreateSeasonCommand
    public partial CreateSeasonCommand ToCreateCommand(CreateSeasonDto dto);

    // Map from UpdateSeasonDto to UpdateSeasonCommand
    public partial UpdateSeasonCommand ToUpdateCommand(UpdateSeasonDto dto);
}