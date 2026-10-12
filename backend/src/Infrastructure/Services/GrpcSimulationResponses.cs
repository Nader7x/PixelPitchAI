using Application.Dtos;
using Domain.Models;

namespace Infrastructure.Services;

/// <summary>Projects persisted Pipeline B lifecycle and results onto the existing API contract.</summary>
public static class GrpcSimulationResponses
{
    public static bool IsLocal(Match match) => match.SimulationId == $"grpc_{match.Id}";

    private static string MapStatus(Match match) => match.MatchStatus switch
    {
        "Queued" => "pending",
        "Completed" => "completed",
        "Failed" or "Truncated" => "failed",
        _ => "running"
    };

    private static string? Error(Match match) => match.MatchStatus switch
    {
        "Truncated" => "The match stream ended before successful completion. Partial results are available.",
        "Failed" => "The simulation could not be started.",
        _ => null
    };

    public static SimulationStatusResponse Status(Match match) => new()
    {
        SimulationId = match.SimulationId!, Status = MapStatus(match),
        MatchId = match.Id, MatchStatus = match.MatchStatus,
        ProgressPercentage = match.MatchStatus == "Completed" ? 100 : null,
        StartedAt = match.ModelSimulationStartTimeUtc, LastUpdated = match.UpdatedAt,
        Error = Error(match)
    };

    public static SimulationResultResponse Result(Match match)
    {
        var status = MapStatus(match);
        var terminal = status is "completed" or "failed";
        return new SimulationResultResponse
        {
            SimulationId = match.SimulationId!, Status = status, MatchId = match.Id,
            HomeTeamName = match.HomeTeam?.Name ?? match.HomeTeamInMatchName ?? string.Empty,
            AwayTeamName = match.AwayTeam?.Name ?? match.AwayTeamInMatchName ?? string.Empty,
            HomeTeamScore = match.HomeTeamScore, AwayTeamScore = match.AwayTeamScore,
            EventsCount = match.MatchEvents?.TotalEvents ?? 0,
            CompletedAt = terminal ? match.UpdatedAt : null,
            ExecutionTime = terminal && match.ModelSimulationStartTimeUtc is { } start
                ? Math.Max(0, (match.UpdatedAt - start).TotalSeconds) : 0,
            Error = Error(match)
        };
    }
}
