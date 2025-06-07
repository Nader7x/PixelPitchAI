using Application.CQRS.Players.Commands;
using Application.CQRS.Players.Queries;
using Application.Dtos;
using Domain.Models;
using Riok.Mapperly.Abstractions;

namespace Application.Mappers;

[Mapper]
public partial class PlayerMapper
{
    // Map from Player to PlayerDto
    public partial PlayerDto ToDto(Player player);

    // Map from list of Player to list of PlayerDto
    public partial List<PlayerDto> ToDtoList(IEnumerable<Player> players);

    // Map from CreatePlayerDto to CreatePlayerCommand
    public partial CreatePlayerCommand ToCreateCommand(CreatePlayerDto dto);

    // Map from UpdatePlayerDto to UpdatePlayerCommand
    public partial UpdatePlayerCommand ToUpdateCommand(UpdatePlayerDto dto);

    // Map from GetAllPlayersQuery parameters
    public partial GetAllPlayersQuery ToGetAllQuery(string nationality, string position, string? preferredFoot,
        int? teamId);

    // Map from GetPlayerByIdQuery parameter
    public partial GetPlayerByIdQuery ToGetByIdQuery(int id);

    // Map for Delete command
    public partial DeletePlayerCommand ToDeleteCommand(int id);

    public partial Player ToPlayerFromCreate(CreatePlayerCommand request);
}