using Application.CQRS.Players.Commands;
using Application.CQRS.Players.Queries;
using Application.Dtos;
using Domain.Models;

namespace Application.Interfaces;

public interface IPlayerMapper
{
    PlayerDto ToDto(Player player);
    List<PlayerDto> ToDtoList(IEnumerable<Player> players);
    CreatePlayerCommand ToCreateCommand(CreatePlayerDto dto);
    UpdatePlayerCommand ToUpdateCommand(UpdatePlayerDto dto);

    GetAllPlayersQuery ToGetAllQuery(
        string nationality,
        string position,
        string? preferredFoot,
        int? teamId
    );

    GetPlayerByIdQuery ToGetByIdQuery(int id);
    DeletePlayerCommand ToDeleteCommand(int id);
    Player ToPlayerFromCreate(CreatePlayerCommand request);
    void ToPlayerFromUpdate(UpdatePlayerCommand request, Player player);
}
