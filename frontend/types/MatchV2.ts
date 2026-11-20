/**
 * PixelPitchAI V2 Canonical Types (aligned with simulation_v2.proto & .NET 11 MatchEventJsonContext)
 */

export type StreamMode = 'broadcast' | 'instant';
export type PlaybackSpeed = 1 | 2 | 4;
export type ConfidenceTier = 'GREEN' | 'AMBER' | 'RED';

export interface StructuredEventV2 {
  event_index: number;
  possession_phase_id: number;
  possessing_team_side: 0 | 1 | 2; // 0=none, 1=home, 2=away
  actor_slot: number; // 1..11 home, 12..22 away
  recipient_slot: number; // 0 if none
  player_id: number;
  player_name: string;
  action_type: string;
  sub_type: string;
  outcome: string;
  start_x: number; // StatsBomb canonical 0..120
  start_y: number; // StatsBomb canonical 0..80
  end_x: number;
  end_y: number;
  end_z: number;
  under_pressure: boolean;
  body_part: string;
  pass_height: string; // Ground | Low | High
  card: string; // Yellow | SecondYellow | Red | ''
  home_score: number;
  away_score: number;
  xg: number;
  post_shot_xg: number;
  xpass: number;
  xt_delta: number;
  vaep_value: number;
  turnover_risk: number;
  match_clock_seconds?: number;
  period?: number;
  sim_timestamp_ms?: number;
}

export interface PlayerSpatialStateV2 {
  slot: number; // 1..22
  player_id: number;
  x: number; // [0, 120]
  y: number; // [0, 80]
  vx: number;
  vy: number;
  facing_rad: number;
  stamina: number; // 0..1
  has_ball: boolean;
  is_sent_off: boolean;
}

export interface PitchSpatialFrameV2 {
  sim_timestamp_ms: number;
  ball_x: number;
  ball_y: number;
  ball_z: number;
  players: PlayerSpatialStateV2[];
  home_voronoi_control: number;
}

export interface TelemetrySnapshotV2 {
  home_xg: number;
  away_xg: number;
  home_psxg: number;
  away_psxg: number;
  home_xt: number;
  away_xt: number;
  home_possession_pct: number;
  home_ppda: number;
  away_ppda: number;
  home_field_tilt_pct: number;
  momentum_index: number; // [-1.0 away .. +1.0 home]
  home_shots: number;
  away_shots: number;
  home_shots_on_target: number;
  away_shots_on_target: number;
  home_passes: number;
  away_passes: number;
  home_pass_accuracy: number;
  away_pass_accuracy: number;
  home_avg_xpass: number;
  away_avg_xpass: number;
  home_high_turnovers: number;
  away_high_turnovers: number;
  match_clock_seconds?: number;
}

export interface CommentaryCueV2 {
  anchor_event_index: number;
  tier: 'template' | 'llm_broadcast' | 'tactical_insight' | string;
  headline: string;
  body: string;
  salience_score: number;
  language: string;
  provider_id: string;
  model_version: string;
  latency_ms: number;
  verifier_passed: boolean;
  fallback_used: boolean;
  match_clock_seconds?: number;
}

export interface MatchLifecycleV2 {
  status: string;
  period: number;
  match_clock_seconds: number;
  home_score: number;
  away_score: number;
  message?: string;
}

export interface KeyMomentBookmark {
  event_index: number;
  match_clock_seconds: number;
  sim_timestamp_ms: number;
  label: string;
  category: 'goal' | 'big_chance' | 'red_card' | 'xt_surge' | 'sub';
  team_side: 0 | 1 | 2;
}

export interface LiveTacticalOverridePayload {
  team_side: 1 | 2;
  formation?: string;
  defensive_line_height?: number;
  pressing_intensity?: number;
  build_up_tempo?: number;
  directness?: number;
  attacking_width?: number;
  defensive_compactness?: number;
  counter_press_vs_regroup?: number;
  substitution?: {
    out_slot: number;
    in_player_id: number;
  };
}

export interface OodGuardrail {
  ood_score: number;
  confidence_tier: ConfidenceTier;
  mahalanobis_distance: number;
  triggered_checks: string[];
  support_notes: string;
}

export interface MetricSummary {
  mean: number;
  std?: number;
  ci95?: [number, number];
  p_value?: number;
}

export interface ExperimentArmResult {
  arm_id: string;
  label?: string;
  outcome_probs: {
    home_win: number;
    draw: number;
    away_win: number;
  };
  expected_points: number;
  metrics_summary: {
    home_xg: MetricSummary;
    away_xg: MetricSummary;
    home_xt?: MetricSummary;
    home_ppda?: MetricSummary;
    home_field_tilt?: MetricSummary;
  };
  delta_vs_baseline?: {
    home_win_prob_delta: number;
    expected_points_delta: number;
    home_xg_delta: MetricSummary;
    away_xg_delta: MetricSummary;
  };
  explainable_diagnostics?: {
    zone_progression_delta_pct?: {
      zone_14_entries_per_90: number;
      left_half_space_entries_per_90: number;
      right_half_space_entries_per_90: number;
    };
    high_turnovers_forced_delta_per_90?: number;
    counters_conceded_behind_line_delta_per_90?: number;
    xt_by_role?: Array<{
      slot: number;
      role: string;
      xt_added_mean: number;
      delta: number;
    }>;
    shot_quality_distribution?: {
      home_big_chances_xg_gt_030_per_90: number;
      away_big_chances_xg_gt_030_per_90: number;
    };
    representative_match_seeds?: Array<{
      seed: number;
      score: string;
      tag: string;
    }>;
  };
  ood_guardrail: OodGuardrail;
}

export interface MonteCarloExperimentResponse {
  experiment_id: string;
  status: 'queued' | 'running' | 'completed' | 'failed';
  execution_tier: string;
  n_simulations: number;
  execution_time_ms: number;
  arms: ExperimentArmResult[];
  tactical_briefing?: {
    tier: string;
    headline: string;
    summary: string;
  };
}

export interface ExperimentProgressEvent {
  experiment_id: string;
  completed_runs: number;
  total_runs: number;
  eta_seconds?: number;
  preliminary_delta_mean?: number;
  status: string;
}

export interface TacticalInterventionPatch {
  target: string;
  value: number | string;
}

export interface MonteCarloExperimentRequest {
  experiment_id?: string;
  question_type: string;
  compute_target?: 'auto' | 'local_cpu_onnx' | 'local_cuda' | 'remote_gpu';
  execution_mode?: 'sync' | 'async';
  n_simulations: number;
  use_paired_crn: boolean;
  base_seed: number;
  sample_player_epistemic_uncertainty?: boolean;
  lock_game_state_adaptivity?: number;
  allow_ood_extrapolation?: boolean;
  baseline_config: Record<string, unknown>;
  interventions: Array<{
    arm_id: string;
    label: string;
    patches: TacticalInterventionPatch[];
  }>;
  diagnostics_options?: {
    compute_zone_progression?: boolean;
    compute_turnover_heatmaps?: boolean;
    compute_role_contributions?: boolean;
    include_ood_check?: boolean;
    top_representative_seeds?: number;
    generate_llm_briefing?: boolean;
    commentary_provider?: string;
  };
}

/**
 * Normalize either compact wire spatial frame `{ t, b, p, hc }` or full/camelCase DTO
 * into canonical `PitchSpatialFrameV2`.
 */
export function normalizeSpatialFrame(raw: Record<string, any>): PitchSpatialFrameV2 {
  if (Array.isArray(raw.b) && Array.isArray(raw.p)) {
    return {
      sim_timestamp_ms: Number(raw.t ?? 0),
      ball_x: Number(raw.b[0] ?? 60),
      ball_y: Number(raw.b[1] ?? 40),
      ball_z: Number(raw.b[2] ?? 0),
      home_voronoi_control: Number(raw.hc ?? 0.5),
      players: raw.p.map((row: number[]) => ({
        slot: Number(row[0] ?? 1),
        player_id: Number(row[6] ?? 0),
        x: Number(row[1] ?? 60),
        y: Number(row[2] ?? 40),
        vx: Number(row[3] ?? 0),
        vy: Number(row[4] ?? 0),
        facing_rad: Number(row[7] ?? 0),
        stamina: Number(row[5] ?? 1),
        has_ball: Boolean(row[8] ?? false),
        is_sent_off: Boolean(row[9] ?? false),
      })),
    };
  }

  const rawPlayers: any[] = raw.players ?? raw.Players ?? [];
  return {
    sim_timestamp_ms: Number(raw.sim_timestamp_ms ?? raw.simTimestampMs ?? 0),
    ball_x: Number(raw.ball_x ?? raw.ballX ?? 60),
    ball_y: Number(raw.ball_y ?? raw.ballY ?? 40),
    ball_z: Number(raw.ball_z ?? raw.ballZ ?? 0),
    home_voronoi_control: Number(
      raw.home_voronoi_control ?? raw.homeVoronoiControl ?? 0.5
    ),
    players: rawPlayers.map((pl) => ({
      slot: Number(pl.slot ?? pl.Slot ?? 1),
      player_id: Number(pl.player_id ?? pl.playerId ?? 0),
      x: Number(pl.x ?? pl.X ?? 60),
      y: Number(pl.y ?? pl.Y ?? 40),
      vx: Number(pl.vx ?? pl.Vx ?? 0),
      vy: Number(pl.vy ?? pl.Vy ?? 0),
      facing_rad: Number(pl.facing_rad ?? pl.facingRad ?? 0),
      stamina: Number(pl.stamina ?? pl.Stamina ?? 1),
      has_ball: Boolean(pl.has_ball ?? pl.hasBall ?? false),
      is_sent_off: Boolean(pl.is_sent_off ?? pl.isSentOff ?? false),
    })),
  };
}
