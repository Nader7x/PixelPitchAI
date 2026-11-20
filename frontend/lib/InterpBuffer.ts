import { PitchSpatialFrameV2, PlaybackSpeed, StreamMode } from '@/types/MatchV2';

export interface InterpolatedPlayerSlot {
  slot: number;
  x: number; // StatsBomb [0, 120]
  y: number; // StatsBomb [0, 80]
  facingRad: number;
  stamina: number;
  hasBall: boolean;
  isSentOff: boolean;
  active: boolean;
}

export interface InterpolatedPitchState {
  simTimestampMs: number;
  ballX: number;
  ballY: number;
  ballZ: number;
  homeVoronoiControl: number;
  players: InterpolatedPlayerSlot[]; // Always length 22 (index 0..21 => slot 1..22)
}

interface BallFlightArc {
  startMs: number;
  durationMs: number;
  peakHeight: number;
  endZ: number;
}

const RING_CAPACITY = 256;
const NUM_PLAYERS = 22;

/**
 * Zero-React-setState Hermite interpolation ring buffer for 60 FPS 3D rendering.
 * Pre-allocates output structures so `sample()` performs zero heap allocations per frame.
 */
export class InterpBuffer {
  private readonly frames: Array<PitchSpatialFrameV2 | null> = new Array(
    RING_CAPACITY
  ).fill(null);
  private head = 0; // next write index
  private count = 0;

  private lookaheadMs: number;
  private speed: PlaybackSpeed = 1;
  private paused = false;

  // Playback cursor synchronized to incoming sim_timestamp_ms
  private playheadSimMs = 0;
  private lastWallMs = 0;
  private initialized = false;

  private activeArc: BallFlightArc | null = null;

  // Pre-allocated sample output to avoid GC pressure inside R3F useFrame()
  private readonly output: InterpolatedPitchState = {
    simTimestampMs: 0,
    ballX: 60,
    ballY: 40,
    ballZ: 0.22,
    homeVoronoiControl: 0.5,
    players: Array.from({ length: NUM_PLAYERS }, (_, i) => ({
      slot: i + 1,
      x: 60,
      y: 40,
      facingRad: 0,
      stamina: 1,
      hasBall: false,
      isSentOff: false,
      active: false,
    })),
  };

  constructor(mode: StreamMode = 'broadcast') {
    this.lookaheadMs = mode === 'broadcast' ? 1500 : 150;
  }

  public setMode(mode: StreamMode): void {
    this.lookaheadMs = mode === 'broadcast' ? 1500 : 150;
  }

  public setLookaheadMs(ms: number): void {
    this.lookaheadMs = Math.max(0, ms);
  }

  public setSpeed(speed: PlaybackSpeed): void {
    this.speed = speed;
  }

  public setPaused(paused: boolean): void {
    this.paused = paused;
    if (!paused) {
      this.lastWallMs = performance.now();
    }
  }

  public isPaused(): boolean {
    return this.paused;
  }

  public seekToSimMs(simMs: number): void {
    this.playheadSimMs = Math.max(0, simMs);
    this.lastWallMs = performance.now();
  }

  public clear(): void {
    this.frames.fill(null);
    this.head = 0;
    this.count = 0;
    this.playheadSimMs = 0;
    this.initialized = false;
    this.activeArc = null;
  }

  /**
   * Register a discrete ball-flight parabolic arc (from StructuredEventV2 pass_height / end_z).
   */
  public triggerBallArc(
    startMs: number,
    durationMs: number,
    passHeight: string,
    endZ: number
  ): void {
    const h = passHeight.toLowerCase();
    let peakHeight = 0.25;
    if (h.includes('high') || h.includes('loft')) {
      peakHeight = Math.max(2.6, endZ + 1.5);
    } else if (h.includes('low') || h.includes('driven')) {
      peakHeight = Math.max(0.85, endZ + 0.4);
    } else if (endZ > 0.3) {
      peakHeight = endZ * 1.15;
    }
    this.activeArc = {
      startMs,
      durationMs: Math.max(180, durationMs),
      peakHeight,
      endZ,
    };
  }

  public push(frame: PitchSpatialFrameV2): void {
    this.frames[this.head] = frame;
    this.head = (this.head + 1) % RING_CAPACITY;
    if (this.count < RING_CAPACITY) {
      this.count++;
    }

    if (!this.initialized) {
      this.playheadSimMs = Math.max(0, frame.sim_timestamp_ms - this.lookaheadMs);
      this.lastWallMs = performance.now();
      this.initialized = true;
    }
  }

  public getLatestSimMs(): number {
    if (this.count === 0) return 0;
    const latestIdx = (this.head - 1 + RING_CAPACITY) % RING_CAPACITY;
    return this.frames[latestIdx]?.sim_timestamp_ms ?? 0;
  }

  public getCurrentPlayheadMs(): number {
    return this.playheadSimMs;
  }

  /**
   * Sample interpolated 22-player + 3D ball state at current wall clock.
   * Called at 60 FPS inside R3F `useFrame()`.
   */
  public sample(nowWallMs: number = performance.now()): InterpolatedPitchState {
    if (this.count === 0) {
      return this.output;
    }

    if (!this.paused) {
      const dtWall = Math.min(100, Math.max(0, nowWallMs - this.lastWallMs));
      this.playheadSimMs += dtWall * this.speed;

      // Gently clamp playhead to stay within lookahead window of latest keyframe
      const latestSimMs = this.getLatestSimMs();
      const targetMaxMs = Math.max(0, latestSimMs - this.lookaheadMs * 0.5);
      if (this.playheadSimMs > latestSimMs) {
        this.playheadSimMs = latestSimMs;
      } else if (latestSimMs - this.playheadSimMs > this.lookaheadMs * 2.5) {
        // Catch up if tab was backgrounded
        this.playheadSimMs = targetMaxMs;
      }
    }
    this.lastWallMs = nowWallMs;

    const targetMs = this.playheadSimMs;
    const { prev, next } = this.findBracketingFrames(targetMs);
    if (!prev) {
      return this.output;
    }

    if (!next || next.sim_timestamp_ms <= prev.sim_timestamp_ms) {
      this.copySingleFrame(prev);
      return this.output;
    }

    const dtMs = next.sim_timestamp_ms - prev.sim_timestamp_ms;
    const dtSec = dtMs / 1000;
    const t = Math.min(1, Math.max(0, (targetMs - prev.sim_timestamp_ms) / dtMs));

    // Cubic Hermite basis functions
    const t2 = t * t;
    const t3 = t2 * t;
    const h00 = 2 * t3 - 3 * t2 + 1;
    const h10 = t3 - 2 * t2 + t;
    const h01 = -2 * t3 + 3 * t2;
    const h11 = t3 - t2;

    this.output.simTimestampMs = targetMs;
    this.output.ballX = prev.ball_x + (next.ball_x - prev.ball_x) * t;
    this.output.ballY = prev.ball_y + (next.ball_y - prev.ball_y) * t;
    this.output.homeVoronoiControl =
      prev.home_voronoi_control +
      (next.home_voronoi_control - prev.home_voronoi_control) * t;

    // 3D parabolic ball arc: z(tau) = 4 * h_peak * tau * (1 - tau) + tau * end_z
    let ballZ = prev.ball_z + (next.ball_z - prev.ball_z) * t;
    if (this.activeArc) {
      const elapsed = targetMs - this.activeArc.startMs;
      if (elapsed >= 0 && elapsed <= this.activeArc.durationMs) {
        const tau = elapsed / this.activeArc.durationMs;
        const arcZ =
          4 * this.activeArc.peakHeight * tau * (1 - tau) +
          tau * this.activeArc.endZ;
        ballZ = Math.max(ballZ, arcZ);
      } else if (elapsed > this.activeArc.durationMs) {
        this.activeArc = null;
      }
    }
    this.output.ballZ = Math.max(0.22, ballZ);

    // Reset active flags
    for (let i = 0; i < NUM_PLAYERS; i++) {
      this.output.players[i].active = false;
    }

    // Interpolate each player present in prev
    for (let i = 0; i < prev.players.length; i++) {
      const p0 = prev.players[i];
      const idx = p0.slot - 1;
      if (idx < 0 || idx >= NUM_PLAYERS) continue;

      let p1 = p0;
      for (let j = 0; j < next.players.length; j++) {
        if (next.players[j].slot === p0.slot) {
          p1 = next.players[j];
          break;
        }
      }

      const out = this.output.players[idx];
      out.slot = p0.slot;
      out.x =
        h00 * p0.x +
        h10 * (p0.vx * dtSec) +
        h01 * p1.x +
        h11 * (p1.vx * dtSec);
      out.y =
        h00 * p0.y +
        h10 * (p0.vy * dtSec) +
        h01 * p1.y +
        h11 * (p1.vy * dtSec);
      out.facingRad = p0.facing_rad + (p1.facing_rad - p0.facing_rad) * t;
      out.stamina = p0.stamina + (p1.stamina - p0.stamina) * t;
      out.hasBall = t < 0.5 ? p0.has_ball : p1.has_ball;
      out.isSentOff = p1.is_sent_off;
      out.active = !p1.is_sent_off;
    }

    return this.output;
  }

  private findBracketingFrames(targetMs: number): {
    prev: PitchSpatialFrameV2 | null;
    next: PitchSpatialFrameV2 | null;
  } {
    const oldestIdx =
      this.count < RING_CAPACITY ? 0 : this.head;

    let prev: PitchSpatialFrameV2 | null = null;
    let next: PitchSpatialFrameV2 | null = null;

    for (let i = 0; i < this.count; i++) {
      const idx = (oldestIdx + i) % RING_CAPACITY;
      const f = this.frames[idx];
      if (!f) continue;

      if (f.sim_timestamp_ms <= targetMs) {
        prev = f;
      } else {
        next = f;
        break;
      }
    }

    if (!prev && this.count > 0) {
      prev = this.frames[oldestIdx];
    }
    return { prev, next };
  }

  private copySingleFrame(frame: PitchSpatialFrameV2): void {
    this.output.simTimestampMs = frame.sim_timestamp_ms;
    this.output.ballX = frame.ball_x;
    this.output.ballY = frame.ball_y;
    this.output.ballZ = Math.max(0.22, frame.ball_z);
    this.output.homeVoronoiControl = frame.home_voronoi_control;

    for (let i = 0; i < NUM_PLAYERS; i++) {
      this.output.players[i].active = false;
    }
    for (let i = 0; i < frame.players.length; i++) {
      const p = frame.players[i];
      const idx = p.slot - 1;
      if (idx < 0 || idx >= NUM_PLAYERS) continue;
      const out = this.output.players[idx];
      out.slot = p.slot;
      out.x = p.x;
      out.y = p.y;
      out.facingRad = p.facing_rad;
      out.stamina = p.stamina;
      out.hasBall = p.has_ball;
      out.isSentOff = p.is_sent_off;
      out.active = !p.is_sent_off;
    }
  }
}
