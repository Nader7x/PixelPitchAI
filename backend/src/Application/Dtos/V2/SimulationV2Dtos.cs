namespace Application.Dtos.V2;

public sealed record StructuredEventV2Dto(
    uint EventIndex,
    uint PossessionPhaseId,
    uint PossessingTeamSide,
    uint ActorSlot,
    uint RecipientSlot,
    uint PlayerId,
    string PlayerName,
    string ActionType,
    string SubType,
    string Outcome,
    float StartX,
    float StartY,
    float EndX,
    float EndY,
    float EndZ,
    bool UnderPressure,
    string BodyPart,
    string PassHeight,
    string Card,
    uint HomeScore,
    uint AwayScore,
    float Xg,
    float PostShotXg,
    float Xpass,
    float XtDelta,
    float VaepValue,
    float TurnoverRisk,
    uint MatchClockSeconds = 0,
    uint Period = 1
);

public sealed record PlayerSpatialStateV2Dto(
    uint Slot,
    uint PlayerId,
    float X,
    float Y,
    float Vx,
    float Vy,
    float FacingRad,
    float Stamina,
    bool HasBall,
    bool IsSentOff
);

public sealed record SpatialFrameV2Dto(
    ulong SimTimestampMs,
    float BallX,
    float BallY,
    float BallZ,
    float HomeVoronoiControl,
    IReadOnlyList<PlayerSpatialStateV2Dto> Players
);

public sealed record TelemetrySnapshotV2Dto(
    float HomeXg,
    float AwayXg,
    float HomePsxg,
    float AwayPsxg,
    float HomeXt,
    float AwayXt,
    float HomePossessionPct,
    float HomePpda,
    float AwayPpda,
    float HomeFieldTiltPct,
    float MomentumIndex,
    uint HomeShots,
    uint AwayShots,
    uint HomeShotsOnTarget,
    uint AwayShotsOnTarget,
    uint HomePasses,
    uint AwayPasses,
    float HomePassAccuracy,
    float AwayPassAccuracy,
    float HomeAvgXpass,
    float AwayAvgXpass,
    uint HomeHighTurnovers,
    uint AwayHighTurnovers
);

public sealed record CommentaryCueV2Dto(
    uint AnchorEventIndex,
    string Tier,
    string Headline,
    string Body,
    float SalienceScore,
    string Language,
    string ProviderId,
    string ModelVersion,
    uint LatencyMs,
    bool VerifierPassed,
    bool FallbackUsed
);

public sealed record MatchLifecycleV2Dto(
    string MatchId,
    string Status,
    uint MatchClockSeconds,
    uint Period,
    uint HomeScore,
    uint AwayScore,
    string? Message = null
);

public sealed record TacticalInstructionsV2Dto(
    float DefensiveLineHeight = 0.50f,
    float PressingIntensity = 0.50f,
    float BuildUpTempo = 0.50f,
    float Directness = 0.45f,
    float AttackingWidth = 0.50f,
    float DefensiveCompactness = 0.50f,
    float CounterPressVsRegroup = 0.50f,
    float CounterAttackUrgency = 0.50f,
    float PossessionRiskAppetite = 0.50f,
    float GameStateAdaptivity = 0.50f
);

public sealed record FormationConfigV2Dto(
    string BaseName = "4-3-3",
    string? BuildUp = null,
    string? InPossession = null,
    string? HighPress = null,
    string? OutOfPossession = null
);

public sealed record StartingPlayerV2Dto(
    int SlotIndex,
    int PlayerId,
    int ShirtNumber,
    string PreferredFoot = "Right",
    string BasePosition = "CM",
    string TacticalRole = "BoxToBoxMidfielder",
    string? DisplayName = null
);

public sealed record BenchPlayerV2Dto(
    int PlayerId,
    int ShirtNumber,
    string PreferredFoot = "Right",
    IReadOnlyList<string>? NaturalPositions = null,
    string DefaultRole = "BoxToBoxMidfielder",
    string? DisplayName = null
);

public sealed record SubstitutionPolicyV2Dto(
    string Mode = "rule_based",
    int MaxSubstitutions = 5,
    int MaxWindows = 3,
    float StaminaSubThreshold = 0.48f
);

public sealed record TeamMatchConfigV2Dto(
    int TeamId,
    string TeamName,
    FormationConfigV2Dto? Formation = null,
    TacticalInstructionsV2Dto? TacticalInstructions = null,
    IReadOnlyList<StartingPlayerV2Dto>? StartingXi = null,
    IReadOnlyList<BenchPlayerV2Dto>? Bench = null,
    SubstitutionPolicyV2Dto? SubstitutionPolicy = null
);

public sealed record MatchConfigurationV2Dto(
    int MatchId,
    int RandomSeed,
    TeamMatchConfigV2Dto Home,
    TeamMatchConfigV2Dto Away
);

public sealed record CommentaryOptionsV2Dto(
    bool Enabled = true,
    string Provider = "ab_split",
    string Language = "en"
);

public sealed record SimulateMatchV2RequestDto(
    int HomeTeamId,
    int AwayTeamId,
    int? HomeTeamSeasonId = null,
    int? AwayTeamSeasonId = null,
    string StreamMode = "broadcast",
    string ComputeTarget = "auto",
    float PlaybackSpeed = 1.0f,
    int SpatialHz = 5,
    CommentaryOptionsV2Dto? Commentary = null,
    MatchConfigurationV2Dto? MatchConfig = null
);

public sealed record SimulateMatchV2ResponseDto(
    bool Succeeded,
    int MatchId,
    string StreamUrl,
    string Status,
    string ComputeTarget,
    string? Error = null
);

public sealed record LiveTacticalOverrideDto(
    string TeamSide,
    float? DefensiveLineHeight = null,
    float? PressingIntensity = null,
    float? BuildUpTempo = null,
    float? Directness = null,
    float? AttackingWidth = null,
    float? DefensiveCompactness = null,
    float? CounterPressVsRegroup = null,
    float? CounterAttackUrgency = null,
    float? PossessionRiskAppetite = null,
    string? FormationName = null,
    int? OutSlotIndex = null,
    int? InPlayerId = null
);

public sealed record LiveTacticalOverrideResponseDto(
    bool Accepted,
    int MatchId,
    string? Message = null
);

public sealed record MatchReplayV2ResponseDto(
    int MatchId,
    string? SpatialTrackObjectKey,
    string? SpatialTrackCodec,
    string? SpatialTrackDownloadUrl,
    string? ModelVersion,
    double? HomeXg,
    double? AwayXg,
    double? HomeXt,
    double? AwayXt,
    string EventsJson
);

public sealed record TacticalPatchDto(
    string Target,
    float Value
);

public sealed record InterventionArmDto(
    string ArmId,
    string Label,
    IReadOnlyList<TacticalPatchDto> Patches
);

public sealed record ExperimentDiagnosticsOptionsDto(
    bool ComputeZoneProgression = true,
    bool ComputeTurnoverHeatmaps = true,
    bool ComputeRoleContributions = true,
    bool IncludeOodCheck = true,
    int TopRepresentativeSeeds = 3,
    bool GenerateLlmBriefing = true,
    string CommentaryProvider = "ab_split"
);

public sealed record MonteCarloExperimentRequestDto(
    string? ExperimentId,
    string QuestionType,
    string ComputeTarget = "auto",
    string ExecutionMode = "async",
    int NSimulations = 100,
    bool UsePairedCrn = true,
    int BaseSeed = 42000,
    bool SamplePlayerEpistemicUncertainty = true,
    float LockGameStateAdaptivity = 0.0f,
    bool AllowOodExtrapolation = false,
    MatchConfigurationV2Dto? BaselineConfig = null,
    IReadOnlyList<InterventionArmDto>? Interventions = null,
    ExperimentDiagnosticsOptionsDto? DiagnosticsOptions = null,
    string? Title = null
);

public sealed record MetricDistributionDto(
    double Mean,
    double? Std = null,
    IReadOnlyList<double>? Ci95 = null,
    double? PValue = null
);

public sealed record OutcomeProbabilitiesDto(
    double HomeWin,
    double Draw,
    double AwayWin
);

public sealed record OodGuardrailDto(
    double OodScore,
    string ConfidenceTier,
    double MahalanobisDistance,
    IReadOnlyList<string> TriggeredChecks,
    string SupportNotes
);

public sealed record RepresentativeMatchSeedDto(
    int Seed,
    string Score,
    string Tag
);

public sealed record ExperimentArmResultDto(
    string ArmId,
    OutcomeProbabilitiesDto OutcomeProbs,
    double ExpectedPoints,
    OodGuardrailDto? OodGuardrail = null,
    IReadOnlyList<RepresentativeMatchSeedDto>? RepresentativeMatchSeeds = null
);

public sealed record TacticalBriefingDto(
    string Tier,
    string Headline,
    string Summary
);

public sealed record MonteCarloExperimentResponseDto(
    string ExperimentId,
    string Status,
    string ExecutionTier,
    int NSimulations,
    long ExecutionTimeMs,
    IReadOnlyList<ExperimentArmResultDto> Arms,
    TacticalBriefingDto? TacticalBriefing = null,
    string? StreamUrl = null,
    string? Error = null
);

public sealed record ExperimentProgressDto(
    string ExperimentId,
    string Status,
    int CompletedRuns,
    int TotalRuns,
    double ProgressPct,
    string? ExecutionTier = null,
    string? Message = null
);

public sealed record ExperimentCompletedRabbitMqEventDto(
    string ExperimentId,
    string UserId,
    string Status,
    string? ExecutionTier,
    long ExecutionTimeMs,
    string? ResultJson,
    string? CommentaryTelemetryJson,
    string? RepresentativeSeedsJson,
    string? ErrorMessage
);

public sealed record NotificationV2Dto(
    string Id,
    string UserId,
    string Type,
    string Title,
    string Content,
    DateTime TimestampUtc,
    bool IsRead = false,
    string? MatchId = null,
    string? ExperimentId = null
);
