'use client';

import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import experimentServiceV2 from '@/Services/ExperimentServiceV2';
import { useExperimentStreamV2 } from '@/app/hooks/useExperimentStreamV2';
import {
  ConfidenceTier,
  ExperimentArmResult,
  MonteCarloExperimentRequest,
  MonteCarloExperimentResponse,
} from '@/types/MatchV2';

export const dynamic = 'force-dynamic';

const BADGE_STYLES: Record<ConfidenceTier, string> = {
  GREEN: 'bg-emerald-500/20 text-emerald-300 border-emerald-500/40',
  AMBER: 'bg-amber-500/20 text-amber-300 border-amber-500/40',
  RED: 'bg-red-500/20 text-red-300 border-red-500/40',
};

export default function CounterfactualExperimentsPage() {
  const router = useRouter();

  const [homeTeamName, setHomeTeamName] = useState('Barcelona');
  const [awayTeamName, setAwayTeamName] = useState('Real Madrid');
  const [nSimulations, setNSimulations] = useState(100);
  const [computeTarget, setComputeTarget] = useState<
    'auto' | 'local_cpu_onnx' | 'local_cuda' | 'remote_gpu'
  >('auto');

  // Variant sliders
  const [defensiveLineHeight, setDefensiveLineHeight] = useState(0.85);
  const [pressingIntensity, setPressingIntensity] = useState(0.84);
  const [counterPress, setCounterPress] = useState(0.88);
  const [directness, setDirectness] = useState(0.45);
  const [buildUpTempo, setBuildUpTempo] = useState(0.65);

  const [activeExperimentId, setActiveExperimentId] = useState<string | null>(
    null
  );
  const [syncResult, setSyncResult] =
    useState<MonteCarloExperimentResponse | null>(null);
  const [isRunning, setIsRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [launchingSeed, setLaunchingSeed] = useState<number | null>(null);

  const { progress, result: streamResult } =
    useExperimentStreamV2(activeExperimentId);
  const experimentResult = streamResult ?? syncResult;

  const handleRunExperiment = async () => {
    setIsRunning(true);
    setError(null);
    setSyncResult(null);
    setActiveExperimentId(null);

    const request: MonteCarloExperimentRequest = {
      question_type: 'tactical_block_comparison',
      compute_target: computeTarget,
      execution_mode: nSimulations > 100 ? 'async' : 'sync',
      n_simulations: nSimulations,
      use_paired_crn: true,
      base_seed: 42000,
      lock_game_state_adaptivity: 0.0,
      baseline_config: {
        home_team: homeTeamName,
        away_team: awayTeamName,
        defensive_line_height: 0.5,
        pressing_intensity: 0.5,
        counter_press_vs_regroup: 0.5,
        directness: 0.45,
        build_up_tempo: 0.5,
      },
      interventions: [
        {
          arm_id: 'variant_tactical',
          label: 'Counterfactual Tactical Variant',
          patches: [
            {
              target: 'home.tactical_instructions.defensive_line_height',
              value: defensiveLineHeight,
            },
            {
              target: 'home.tactical_instructions.pressing_intensity',
              value: pressingIntensity,
            },
            {
              target: 'home.tactical_instructions.counter_press_vs_regroup',
              value: counterPress,
            },
            {
              target: 'home.tactical_instructions.directness',
              value: directness,
            },
            {
              target: 'home.tactical_instructions.build_up_tempo',
              value: buildUpTempo,
            },
          ],
        },
      ],
      diagnostics_options: {
        compute_zone_progression: true,
        compute_turnover_heatmaps: true,
        compute_role_contributions: true,
        include_ood_check: true,
        top_representative_seeds: 3,
        generate_llm_briefing: true,
      },
    };

    try {
      const res = await experimentServiceV2.runExperiment(request);
      if (res.status === 'completed') {
        setSyncResult(res);
      } else {
        setActiveExperimentId(res.experiment_id);
      }
    } catch (err) {
      setError(
        err instanceof Error ? err.message : 'Failed to run experiment'
      );
    } finally {
      setIsRunning(false);
    }
  };

  const handleWatchSeedIn3D = async (seed: number, armId: string) => {
    if (!experimentResult) return;
    setLaunchingSeed(seed);
    try {
      const { matchId } = await experimentServiceV2.replaySeedIn3D(
        experimentResult.experiment_id,
        seed,
        armId
      );
      if (typeof window !== 'undefined') {
        localStorage.setItem('matchId', String(matchId));
      }
      router.push(`/simulationview/${matchId}`);
    } catch (err) {
      setError(
        err instanceof Error ? err.message : 'Failed to launch 3D seed replay'
      );
    } finally {
      setLaunchingSeed(null);
    }
  };

  const renderArmCard = (arm: ExperimentArmResult, isBaseline: boolean) => {
    const hw = arm.outcome_probs.home_win * 100;
    const dr = arm.outcome_probs.draw * 100;
    const aw = arm.outcome_probs.away_win * 100;
    const tier = arm.ood_guardrail?.confidence_tier ?? 'GREEN';

    return (
      <div
        key={arm.arm_id}
        className="rounded-2xl border border-white/15 bg-slate-900/80 p-5"
      >
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div>
            <span className="text-xs font-semibold tracking-wider text-gray-400 uppercase">
              {isBaseline ? 'Baseline Arm' : 'Counterfactual Variant'}
            </span>
            <h3 className="text-lg font-bold text-white">
              {arm.label || arm.arm_id}
            </h3>
          </div>
          <span
            className={`rounded-full border px-3 py-1 text-xs font-bold ${BADGE_STYLES[tier]}`}
            title={arm.ood_guardrail?.support_notes}
          >
            OOD: {tier} ({(arm.ood_guardrail?.ood_score ?? 0).toFixed(2)})
          </span>
        </div>

        {/* Win / Draw / Loss Probability Bar */}
        <div className="mt-4">
          <div className="mb-1.5 flex justify-between text-xs font-medium text-gray-300">
            <span className="text-blue-400">Home Win {hw.toFixed(1)}%</span>
            <span className="text-gray-400">Draw {dr.toFixed(1)}%</span>
            <span className="text-red-400">Away Win {aw.toFixed(1)}%</span>
          </div>
          <div className="flex h-4 w-full overflow-hidden rounded-full bg-slate-800">
            <div className="bg-blue-500" style={{ width: `${hw}%` }} />
            <div className="bg-slate-500" style={{ width: `${dr}%` }} />
            <div className="bg-red-500" style={{ width: `${aw}%` }} />
          </div>
        </div>

        {/* Key Metrics */}
        <div className="mt-4 grid grid-cols-2 gap-3 text-sm sm:grid-cols-4">
          <div className="rounded-xl bg-slate-950/70 p-3">
            <div className="text-xs text-gray-400">Expected Pts</div>
            <div className="mt-1 font-mono text-lg font-bold text-emerald-400">
              {arm.expected_points.toFixed(2)}
            </div>
            {arm.delta_vs_baseline && (
              <div className="font-mono text-xs text-emerald-300">
                {arm.delta_vs_baseline.expected_points_delta >= 0 ? '+' : ''}
                {arm.delta_vs_baseline.expected_points_delta.toFixed(3)}
              </div>
            )}
          </div>
          <div className="rounded-xl bg-slate-950/70 p-3">
            <div className="text-xs text-gray-400">Home xG</div>
            <div className="mt-1 font-mono text-lg font-bold text-blue-400">
              {arm.metrics_summary.home_xg.mean.toFixed(2)}
            </div>
            {arm.delta_vs_baseline && (
              <div className="font-mono text-xs text-blue-300">
                {arm.delta_vs_baseline.home_xg_delta.mean >= 0 ? '+' : ''}
                {arm.delta_vs_baseline.home_xg_delta.mean.toFixed(2)}
              </div>
            )}
          </div>
          <div className="rounded-xl bg-slate-950/70 p-3">
            <div className="text-xs text-gray-400">Away xG</div>
            <div className="mt-1 font-mono text-lg font-bold text-red-400">
              {arm.metrics_summary.away_xg.mean.toFixed(2)}
            </div>
            {arm.delta_vs_baseline && (
              <div className="font-mono text-xs text-red-300">
                {arm.delta_vs_baseline.away_xg_delta.mean >= 0 ? '+' : ''}
                {arm.delta_vs_baseline.away_xg_delta.mean.toFixed(2)}
              </div>
            )}
          </div>
          <div className="rounded-xl bg-slate-950/70 p-3">
            <div className="text-xs text-gray-400">PPDA / Tilt</div>
            <div className="mt-1 font-mono text-sm font-bold text-white">
              {(arm.metrics_summary.home_ppda?.mean ?? 0).toFixed(1)} /{' '}
              {((arm.metrics_summary.home_field_tilt?.mean ?? 0.5) * 100).toFixed(0)}%
            </div>
          </div>
        </div>

        {/* OOD Notes */}
        {arm.ood_guardrail?.support_notes && (
          <p className="mt-3 text-xs text-gray-400">
            {arm.ood_guardrail.support_notes}
          </p>
        )}

        {/* Representative Match Seeds -> Watch Seed in 3D */}
        {arm.explainable_diagnostics?.representative_match_seeds &&
          arm.explainable_diagnostics.representative_match_seeds.length > 0 && (
            <div className="mt-4 border-t border-white/10 pt-3">
              <div className="mb-2 text-xs font-semibold text-gray-300">
                Representative Monte Carlo Seeds
              </div>
              <div className="flex flex-wrap gap-2">
                {arm.explainable_diagnostics.representative_match_seeds.map(
                  (rep) => (
                    <button
                      key={rep.seed}
                      type="button"
                      disabled={launchingSeed === rep.seed}
                      onClick={() => handleWatchSeedIn3D(rep.seed, arm.arm_id)}
                      className="rounded-lg border border-emerald-500/40 bg-emerald-500/15 px-3 py-1.5 text-xs font-semibold text-emerald-300 transition hover:bg-emerald-500/30 disabled:opacity-50"
                    >
                      {launchingSeed === rep.seed
                        ? 'Launching...'
                        : `Watch Seed #${rep.seed} in 3D (${rep.score} · ${rep.tag})`}
                    </button>
                  )
                )}
              </div>
            </div>
          )}
      </div>
    );
  };

  return (
    <div className="min-h-screen bg-slate-950 px-6 py-8 text-white">
      <div className="mx-auto max-w-6xl space-y-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <h1 className="text-2xl font-bold tracking-tight">
              Counterfactual Tactical Workbench
            </h1>
            <p className="text-sm text-gray-400">
              Paired Common Random Numbers (CRN) Monte Carlo experimentation with
              OOD guardrails and 3D seed inspection.
            </p>
          </div>
          <Link
            href="/matchsimulation"
            className="rounded-xl border border-white/15 bg-white/5 px-4 py-2 text-sm font-medium hover:bg-white/10"
          >
            ← Match Simulation
          </Link>
        </div>

        {/* Configuration Panel */}
        <div className="grid gap-6 rounded-2xl border border-white/15 bg-slate-900/70 p-6 md:grid-cols-2">
          <div className="space-y-4">
            <h2 className="text-sm font-semibold text-gray-300 uppercase">
              Matchup & Compute
            </h2>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-xs text-gray-400">Home Team</label>
                <input
                  value={homeTeamName}
                  onChange={(e) => setHomeTeamName(e.target.value)}
                  className="mt-1 w-full rounded-lg border border-white/15 bg-slate-950 px-3 py-2 text-sm"
                />
              </div>
              <div>
                <label className="text-xs text-gray-400">Away Team</label>
                <input
                  value={awayTeamName}
                  onChange={(e) => setAwayTeamName(e.target.value)}
                  className="mt-1 w-full rounded-lg border border-white/15 bg-slate-950 px-3 py-2 text-sm"
                />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-xs text-gray-400">
                  Simulations (CRN Pairs)
                </label>
                <select
                  value={nSimulations}
                  onChange={(e) => setNSimulations(Number(e.target.value))}
                  className="mt-1 w-full rounded-lg border border-white/15 bg-slate-950 px-3 py-2 text-sm"
                >
                  <option value={25}>25 paired runs</option>
                  <option value={100}>100 paired runs</option>
                  <option value={250}>250 paired runs (Async SSE)</option>
                </select>
              </div>
              <div>
                <label className="text-xs text-gray-400">Compute Tier</label>
                <select
                  value={computeTarget}
                  onChange={(e) =>
                    setComputeTarget(
                      e.target.value as
                        | 'auto'
                        | 'local_cpu_onnx'
                        | 'local_cuda'
                        | 'remote_gpu'
                    )
                  }
                  className="mt-1 w-full rounded-lg border border-white/15 bg-slate-950 px-3 py-2 text-sm"
                >
                  <option value="auto">Auto Router</option>
                  <option value="remote_gpu">Remote GPU (A100/L4)</option>
                  <option value="local_cuda">Local CUDA</option>
                  <option value="local_cpu_onnx">Local CPU</option>
                </select>
              </div>
            </div>
          </div>

          {/* Tactical Intervention Sliders */}
          <div className="space-y-3">
            <h2 className="text-sm font-semibold text-gray-300 uppercase">
              Variant Tactical Intervention (vs 0.50 Baseline)
            </h2>
            {[
              {
                label: 'Defensive Line Height',
                val: defensiveLineHeight,
                set: setDefensiveLineHeight,
              },
              {
                label: 'Pressing Intensity',
                val: pressingIntensity,
                set: setPressingIntensity,
              },
              {
                label: 'Counter-Press vs Regroup',
                val: counterPress,
                set: setCounterPress,
              },
              { label: 'Directness', val: directness, set: setDirectness },
              {
                label: 'Build-up Tempo',
                val: buildUpTempo,
                set: setBuildUpTempo,
              },
            ].map((s) => (
              <div key={s.label} className="flex items-center gap-3 text-xs">
                <span className="w-44 text-gray-300">{s.label}</span>
                <input
                  type="range"
                  min={0.1}
                  max={0.95}
                  step={0.01}
                  value={s.val}
                  onChange={(e) => s.set(Number(e.target.value))}
                  className="flex-1 accent-emerald-500"
                />
                <span className="w-10 text-right font-mono text-emerald-400">
                  {s.val.toFixed(2)}
                </span>
              </div>
            ))}
            <button
              type="button"
              onClick={handleRunExperiment}
              disabled={isRunning}
              className="mt-2 w-full rounded-xl bg-emerald-500 py-2.5 text-sm font-bold text-black transition hover:bg-emerald-400 disabled:opacity-50"
            >
              {isRunning
                ? 'Running Monte Carlo Experiment...'
                : 'Run Counterfactual Experiment'}
            </button>
          </div>
        </div>

        {/* SSE Async Progress */}
        {progress && !experimentResult && (
          <div className="rounded-xl border border-blue-500/30 bg-blue-950/30 p-4 text-sm">
            <div className="flex justify-between">
              <span>
                Running simulations: {progress.completed_runs} /{' '}
                {progress.total_runs}
              </span>
              {typeof progress.eta_seconds === 'number' && (
                <span className="font-mono text-blue-300">
                  ETA: {progress.eta_seconds}s
                </span>
              )}
            </div>
            <div className="mt-2 h-2 w-full overflow-hidden rounded-full bg-slate-800">
              <div
                className="h-full bg-blue-500 transition-all"
                style={{
                  width: `${(progress.completed_runs / Math.max(1, progress.total_runs)) * 100}%`,
                }}
              />
            </div>
          </div>
        )}

        {error && (
          <div className="rounded-xl border border-red-500/40 bg-red-950/40 p-4 text-sm text-red-200">
            {error}
          </div>
        )}

        {/* Results Comparison */}
        {experimentResult && (
          <div className="space-y-4">
            {experimentResult.tactical_briefing && (
              <div className="rounded-2xl border border-emerald-500/30 bg-emerald-950/20 p-5">
                <div className="text-xs font-semibold text-emerald-400 uppercase">
                  Tactical Analyst Briefing ({experimentResult.execution_tier})
                </div>
                <div className="mt-1 text-base font-bold text-white">
                  {experimentResult.tactical_briefing.headline}
                </div>
                <p className="mt-1 text-sm text-gray-300">
                  {experimentResult.tactical_briefing.summary}
                </p>
              </div>
            )}

            <div className="grid gap-4 lg:grid-cols-2">
              {experimentResult.arms.map((arm, idx) =>
                renderArmCard(arm, idx === 0 || arm.arm_id === 'baseline')
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
