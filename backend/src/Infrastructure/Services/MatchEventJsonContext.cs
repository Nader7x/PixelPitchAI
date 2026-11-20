using System.Text.Json.Serialization;
using Application.Dtos;
using Application.Dtos.V2;
using Domain.Models;

namespace Infrastructure.Services;

/// <summary>
///     Compile-time System.Text.Json Source Generator Context for Native AOT.
///     All V1 and V2 SSE, gRPC, RabbitMQ, and REST hot-path DTOs MUST be registered here.
/// </summary>
[JsonSerializable(typeof(FootballMatchEvent))]
[JsonSerializable(typeof(List<FootballMatchEvent>))]
[JsonSerializable(typeof(FootballMatchEvent[]))]
[JsonSerializable(typeof(StructuredEventV2Dto))]
[JsonSerializable(typeof(List<StructuredEventV2Dto>))]
[JsonSerializable(typeof(PlayerSpatialStateV2Dto))]
[JsonSerializable(typeof(SpatialFrameV2Dto))]
[JsonSerializable(typeof(TelemetrySnapshotV2Dto))]
[JsonSerializable(typeof(CommentaryCueV2Dto))]
[JsonSerializable(typeof(MatchLifecycleV2Dto))]
[JsonSerializable(typeof(TacticalInstructionsV2Dto))]
[JsonSerializable(typeof(FormationConfigV2Dto))]
[JsonSerializable(typeof(StartingPlayerV2Dto))]
[JsonSerializable(typeof(BenchPlayerV2Dto))]
[JsonSerializable(typeof(SubstitutionPolicyV2Dto))]
[JsonSerializable(typeof(TeamMatchConfigV2Dto))]
[JsonSerializable(typeof(MatchConfigurationV2Dto))]
[JsonSerializable(typeof(CommentaryOptionsV2Dto))]
[JsonSerializable(typeof(SimulateMatchV2RequestDto))]
[JsonSerializable(typeof(SimulateMatchV2ResponseDto))]
[JsonSerializable(typeof(LiveTacticalOverrideDto))]
[JsonSerializable(typeof(LiveTacticalOverrideResponseDto))]
[JsonSerializable(typeof(MatchReplayV2ResponseDto))]
[JsonSerializable(typeof(TacticalPatchDto))]
[JsonSerializable(typeof(InterventionArmDto))]
[JsonSerializable(typeof(List<InterventionArmDto>))]
[JsonSerializable(typeof(ExperimentDiagnosticsOptionsDto))]
[JsonSerializable(typeof(MonteCarloExperimentRequestDto))]
[JsonSerializable(typeof(MetricDistributionDto))]
[JsonSerializable(typeof(OutcomeProbabilitiesDto))]
[JsonSerializable(typeof(OodGuardrailDto))]
[JsonSerializable(typeof(RepresentativeMatchSeedDto))]
[JsonSerializable(typeof(ExperimentArmResultDto))]
[JsonSerializable(typeof(TacticalBriefingDto))]
[JsonSerializable(typeof(MonteCarloExperimentResponseDto))]
[JsonSerializable(typeof(ExperimentProgressDto))]
[JsonSerializable(typeof(ExperimentCompletedRabbitMqEventDto))]
[JsonSerializable(typeof(NotificationV2Dto))]
[JsonSerializable(typeof(V1StartMatchRequestPayload))]
[JsonSerializable(typeof(V1RegisterWebhookPayload))]
[JsonSerializable(typeof(V1MatchStatisticsUpdateDto))]
[JsonSerializable(typeof(V1TeamStatisticsDto))]
[JsonSerializable(typeof(V1MatchInfoDto))]
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    GenerationMode = JsonSourceGenerationMode.Default
)]
public partial class MatchEventJsonContext : JsonSerializerContext { }

public sealed record V1StartMatchRequestPayload(
    int match_id,
    int home_team_id,
    int away_team_id,
    string? home_team_name,
    string? away_team_name,
    string? home_team_season,
    string? away_team_season,
    int num_tokens_to_generate,
    double temperature,
    double top_p,
    int top_k,
    int max_new_tokens,
    string webhook_url,
    string webhook_secret
);

public sealed record V1RegisterWebhookPayload(
    string webhook_url,
    string webhook_secret
);

public sealed record V1TeamStatisticsDto(
    string? Name,
    int Score,
    int Shots,
    int ShotsOnTarget,
    int Possession,
    int Passes,
    double PassAccuracy,
    int Corners,
    int Fouls,
    int YellowCards,
    int RedCards,
    int Offsides
);

public sealed record V1MatchInfoDto(
    string? Status,
    bool IsLive,
    int CurrentMinute,
    int LastEventTime,
    string? EventType,
    string? EventTeam
);

public sealed record V1MatchStatisticsUpdateDto(
    int MatchId,
    string? TimeStamp,
    V1TeamStatisticsDto HomeTeam,
    V1TeamStatisticsDto AwayTeam,
    V1MatchInfoDto MatchInfo,
    DateTime LastUpdated
);
