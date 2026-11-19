# PixelPitchAI v2 — Master Revamp Plan: Hybrid Football Event Simulation & Counterfactual Experimentation Platform

**Worktree & Branch:** `D:\programming\GitHub\PixelPitchAI-v2-revamp` on branch `feat/v2-hybrid-simulation-revamp`  
**Synthesized By:** `@engine-integration`  
**Contributing Specifications:**
- Pillar 1 (Data Engineering & Feature Pipeline): [`01_data_and_feature_pipeline.md`](file:///D:/programming/GitHub/PixelPitchAI-v2-revamp/docs/revamp/01_data_and_feature_pipeline.md) (`@worktree-data`)
- Modules A & B (Match Config, Tactical State & Experimentation): [`02_tactical_state_and_experimentation.md`](file:///D:/programming/GitHub/PixelPitchAI-v2-revamp/docs/revamp/02_tactical_state_and_experimentation.md) (`@tactical-state`)
- Modules C & D (Hybrid Event Transformer & Outcome Models): [`03_hybrid_event_transformer_and_models.md`](file:///D:/programming/GitHub/PixelPitchAI-v2-revamp/docs/revamp/03_hybrid_event_transformer_and_models.md) (`@event-transformer`)
- Module E (SSE Presentation, Integration & Supporting LLM): [`04_presentation_and_integration.md`](file:///D:/programming/GitHub/PixelPitchAI-v2-revamp/docs/revamp/04_presentation_and_integration.md) (`@engine-integration`)

---

## 1. Executive Blueprint & Strategic Shift (V1 → V2)

PixelPitchAI v1 established an operational multi-service pipeline (`simulation-engine` Python 3.12 FastAPI/gRPC → `backend` .NET 10 API → PostgreSQL/Redis/RabbitMQ → SSE/Next.js 16 virtual pitch). However, forensic inspection of `D:\programming\GitHub\PixelPitchAI` shows that v1's core simulator—a fine-tuned `124M` GPT-2 model (`gpt2-football-finetuned`, `1024`-token context) paired with a 4-input XGBoost summary regressor (`tuned_xgboost_model.json`) trained on 6 La Liga seasons (`2016–2021`)—cannot support modern tactical realism or coaching experimentation.

PixelPitchAI v2 adopts **Option B: the locked `Base-112M` Custom Football Event Transformer (`PixelPitch-EventTF-112M`, within the 80M–150M target range) embedded inside a Hybrid Stateful Simulation Engine**. It fuses **Approach A** (learned spatio-tactical sequence behavior and continuous player style embeddings) with **Approach B** (explicit Football State Machine, kinematic/off-ball spatial physics, calibrated tabular/MLP outcome physics, and hard legality masks), while reserving general **Language Models strictly as a supporting broadcast commentary and tactical analyst narration layer**—never controlling match physics.

### V1 vs. V2 Architecture Comparison

| Capability Area | PixelPitchAI v1 (Current Baseline) | PixelPitchAI v2 (Locked Target Blueprint) |
| :--- | :--- | :--- |
| **Core Simulation Model** | `GPT2LMHeadModel` (`124M` params, `38.7M` wasted in `50,438` BPE tokens, `n_ctx=1024` wiping history at `keep_last_n=400` and `[SECOND HALF START]`). | **Locked `PixelPitch-EventTF-112M`** (`12L`, `d=768`, `12Q/4KV` GQA, RoPE `500k`, SwiGLU, `16,384` event-step context = full 90/120-min match in one uninterrupted KV cache) + **2L Intra-Event Causal Micro-Decoder**. |
| **Training Corpus & Features** | `6` La Liga seasons (`29` teams, `1,329` players); `174` hardcoded `Team_Year` special tokens; verbose 3-line-per-pass text (`~5,500` lines/match); **no `xG`, no `xT`, no `xPass`, no 360 freeze frames, no lineups**. | **StatsBomb Open (`4,235` matches, `426` w/ 360) + Wyscout Open (`1,941` matches)** (`~10.7M+` receipt-folded events) **plus 2022–2026 Multi-Source Web Scraping** (`FBref`, `Understat`, `SofaScore`/`FotMob`/`WhoScored`, `Transfermarkt`/`EA FC` unified via `player_id_crosswalk.parquet`). |
| **Team, Player & Tactical Conditioning** | 4-input XGBoost (`[teamA, teamB, seasonA, seasonB]`) → 11 coarse English text buckets; `bad_words_ids` suppresses wrong teams; ignores `Coach.PreferredFormation` & `Player` attributes. | **Module A + B**: Full Starting XI (`11` + `12` bench), 4-phase dual formations (`build_up`, `in_possession`, `high_press`, `out_of_possession`), `22` `PlayerRole` priors, `10` `[0,1]` tactical sliders, sub policies, and Empirical-Bayes `24d` (+ `8d` GK/bias) + `64d` style vectors (`sigma_p` uncertainty). |
| **Rules, Coordinates & 22 Players** | Raw StatsBomb per-team coordinate flips causing spatial jumps; post-hoc `MatchParser` & `ZeroAllocationEventParser`; `EventPlotter` renders only last 5 actor ghosts; `FootballPitch.tsx` divides by `100` instead of `120×80`. | **Canonical Global Frame `[0, 120] × [0, 80]`**; **Explicit Football FSM** + hard logit masks (`100%` legal events); **3-Layer GPU 22-Player Off-Ball Spatial Solver** + `24×16` Pitch Control + synthetic `[B,22,7]` 360 freeze frames. |
| **Calibrated Analytics** | No `xG`/`xT`/`xPass` in simulation or `.NET` `MatchStatistics`; goals emerge purely from uncalibrated `~0.12%` token sampling. | **Module D Teacher–Student Pipeline**: Calibrated CatBoost/LightGBM teachers (`xG`, `PSxG`, `xPass`, `xT`, `VAEP`, `Turnover`) + Bayesian log-odds skill residuals, distilled to **GPU-resident MLPs** (`ECE < 0.015`, zero CPU–GPU sync). |
| **Compute & Experimentation** | Single-match text rollout only (`~45 min` CPU / `~30s` GPU); zero counterfactual or causal diagnostics. | **Three-Tier `ComputeBackendRouter`** (`Colab/Cloud A100/L4` `~10–18s/100` CRN runs; `Local CUDA`; `Local CPU INT8 ONNX` `~4–6 ms/event`), answering coaching & recruitment questions with zone/turnover/`xT`/`xG` waterfalls and **4-tier OOD guardrails** (`GREEN`/`AMBER`/`RED`). |
| **Presentation, Storage & Commentary** | Dual SSE + SignalR (`MatchHub`, `/Notify`); `BasicGetAsync` `700ms` RabbitMQ polling; reflection-based anonymous JSON in `BroadcastStatisticsAsync`. | **100% SSE everywhere** (`sse-starlette` + .NET 10 Native AOT `Channel<SseMessage>` including `/api/v2/notifications/stream`, zero SignalR); Zstd spatial tracks in **Cloudflare R2 / Azure Blob** (`ISpatialTrackStorageService`); **Dual `CommentaryProvider`** (Local `3B` + Flash API w/ A/B telemetry + `<1ms` template fallback). |

---

## 2. Unified End-to-End System Architecture

### 2.1 Five-Module Hybrid Engine & Data Lakehouse Topology

```mermaid
flowchart TB
    subgraph Pillar1["Pillar 1: Offline Data Lakehouse & Profilers (01_)"]
        RawData["StatsBomb (4,235) + Wyscout (1,941)\n+ 2022-2026 Scraped (FBref/Understat/SofaScore/TM)"]
        DuckETL["DuckDB + Zstd Parquet ETL\nCanon [0,120]x[0,80] + Crosswalk Resolver"]
        TeacherD["Teacher GBDTs & xT Grid\n(xG, PSxG, xPass, 24x16 xT, VAEP)"]
        Profiler["Empirical-Bayes Profiler\nplayer_profiles_v2.parquet + tactical_manifold_v2.pkl"]
        HFShards["HF Datasets Packed Shards\n(16,384-step varlen blocks)"]
        RawData --> DuckETL --> TeacherD --> HFShards
        DuckETL --> Profiler
    end

    subgraph CoreSim["PixelPitchAI v2 Hybrid Simulation Engine (ComputeBackendRouter)"]
        Router["ComputeBackendRouter\n(Remote A100/L4 | Local CUDA | CPU INT8 ONNX)"]
        ModA["Module A (02_): MatchConfigurationV2\nXI + Bench + 4-Phase Formations + 22 Roles + Tactics"]
        OODGate["4-Tier OOD Guardrail Gate\n(GREEN / AMBER / RED)"]
        ModB["Module B (02_): Tactical State & Spatial Engine\n3-Layer 22-Player Solver + 24x16 Pitch Control"]
        ModC1["Module C1 (03_): Explicit Football FSM\nClock, Score, Restarts, Cards, Hard Grammar Masks"]
        ModC2["Module C2 (03_): PixelPitch-EventTF-112M\n12L GQA + Cross-Attn M_t[28x768] + AdaLN + 2L Micro-Dec"]
        ModD["Module D (03_): Distilled GPU Outcome Heads\nCalibrated xG / PSxG / xPass / xT / VAEP / Turnover"]

        Router --> ModC2
        ModA --> OODGate --> ModB
        ModB <-->|"M_t, s_t, Masks, Spatial Prior"| ModC2
        ModC1 <-->|"Legal Action/Target/XY Masks"| ModC2
        ModC2 -->|"Proposed Event + Coords"| ModD
        ModB -->|"360 Freeze [B,22,7] + Skill Residuals"| ModD
        ModD -->|"Calibrated Outcome + Telemetry"| ModC1
        ModC1 -->|"Committed State S_{t+1}"| ModB
    end

    subgraph ModuleE["Module E: Presentation, Integration & Supporting LLM (04_)"]
        LLM["Dual CommentaryProvider\n(<1ms Templates + Local 3B / Flash API + A/B Log)"]
        Transport["gRPC SimulationServiceV2 (50051)\n+ RabbitMQ Push Bus (match_events_v2)"]
        DotNet[".NET 10 Footex API (Native AOT, Zero SignalR)\nMatchesV2 / Experiments / SSE Broadcaster + Redis + PG"]
        ObjStore["ISpatialTrackStorageService\nCloudflare R2 / Azure Blob (Zstd Tracks)"]
        Frontend["Next.js 16 Client (100% SSE)\n60 FPS 22-Player 3D R3F + 120x80 2D Board + Lab"]

        ModC1 -->|"StructuredEventV2 + SpatialFrameV2"| LLM
        ModC1 --> Transport
        LLM -->|"CommentaryCueV2"| Transport
        Transport --> DotNet -->|"SSE (Match, Experiment & Notifications)"| Frontend
        DotNet -.->|"Upload / Stream Zstd Tracks"| ObjStore
        Frontend -.->|"Stateless POST /tactics"| DotNet
    end

    Profiler --> ModB
    HFShards -->|"Colab A100 4-Stage Training"| ModC2
    TeacherD -->|"Distillation + SHAP Explainability"| ModD
```

### 2.2 Dual Execution Modes: Live Match SSE Stream vs. Batch Monte Carlo

```mermaid
sequenceDiagram
    autonumber
    participant UI as Next.js 16 Frontend
    participant API as .NET 10 Footex API
    participant Eng as Python Hybrid Engine (Modules B+C+D)
    participant LLM as Dual CommentaryProvider

    rect rgb(235, 245, 255)
        Note over UI,LLM: Mode 1: Single Live Match Streaming (100% SSE, 60 FPS Virtual Pitch)
        UI->>API: POST /api/v2/matches/simulate/{userId} (MatchConfigurationV2)
        API->>Eng: gRPC StreamMatchV2 (via ComputeBackendRouter)
        UI->>API: GET /api/v2/matches/{id}/stream (EventSource SSE)
        loop Each Event Step (1.5s Lookahead Buffer)
            Eng->>Eng: Mod B Spatial + Mod C2 Base-112M + Mod D Calibration + FSM
            Eng-->>API: gRPC MatchStreamFrameV2 (StructuredEventV2 + PitchSpatialFrameV2 + Telemetry)
            Eng--)LLM: Async High-Salience Event JSON (local_3b or flash_api A/B)
            LLM-->>Eng: Verified CommentaryCueV2 (120-350ms, template fallback >600ms)
            Eng-->>API: gRPC CommentaryCueV2
            API-->>UI: SSE (match_event / spatial_frame / telemetry / commentary)
        end
        Eng-->>API: End-of-Match Zstd Spatial Track -> R2 / Azure Blob Object Key
        opt Mid-Match User Intervention
            UI->>API: POST /api/v2/matches/{id}/tactics (Line Height / Sub)
            API->>Eng: gRPC ApplyLiveTacticalOverride
        end
    end

    rect rgb(240, 253, 244)
        Note over UI,LLM: Mode 2: Counterfactual Experimentation Platform (100-1,000 Batch GPU Runs)
        UI->>API: POST /api/v2/experiments (Baseline + Intervention Arms)
        API->>Eng: POST /v2/experiments/monte-carlo (Routed to A100/L4 or Local CUDA)
        UI->>API: GET /api/v2/experiments/{id}/stream (SSE Progress)
        Eng->>Eng: Vectorized GPU Rollout (No Event Commentary Overhead)
        Eng->>Eng: Compute Zone Waterfalls, 15s Turnover Cost, SHAP & 4-Tier OOD Checks
        Eng--)LLM: Aggregated Diagnostic JSON (1 Call -> Tier-2B Coach Briefing)
        Eng-->>API: MonteCarloExperimentResponse + Representative Seeds
        API-->>UI: SSE Completed (+ /api/v2/notifications/stream) + "Watch Seed in 3D"
    end
```

---

## 3. Canonical Cross-Module Contracts & Harmonization

To guarantee zero integration friction across Python, `.proto`, `.NET 10`, PostgreSQL, R2/Blob Storage, and TypeScript, all 4 component specs enforce these canonical contracts:

1. **Dual Pitch Coordinate Standard (`[0, 120] × [0, 80]`):**
   - **Canonical Global Frame**: Home attacks `+x` (`0 → 120`), Away attacks `-x` (`120 → 0`), `y ∈ [0, 80]`. Eliminates StatsBomb's per-possession coordinate flip across consecutive events.
   - **Attacking-Relative Frame**: Acting team attacks `+x` (`x_att → 120.0`, goal mouth `y ∈ [36, 44], z ∈ [0, 2.44]`) inside Module D (`xG`, `PSxG`, `xPass`, `xT`) for Home/Away symmetry.
   - **Frontend Fix**: `frontend/app/Components/FootballPitch/FootballPitch.tsx` (lines 252–253) must normalize by `(x / 120) * 700` and `(y / 80) * 420` instead of `/ 100`.
2. **Slot Addressing & Capability Vectors (`ACTOR_SLOT_1_22`, `24d` Core + `8d` GK/Bias Sidecar + `64d` Style):**
   - `slot 0 = SYSTEM`, `1..11 = Home XI`, `12..22 = Away XI`. Substitutions swap player profiles inside the tactical slot (`starting_xi` + `bench <= 12`) so the Transformer vocabulary stays invariant (`vocab=23`) with zero unknown-player OOV issues.
   - `player_profiles_v2.parquet` exports `attributes_24d` + `sigma_24d`, `gk_attributes_8d` (`6` GK attributes `active_gk_attributes_6d` + `2` spatial lane/verticality biases) + `finishing_log_odds`, and `latent_style_64d` as cleanly separated arrays (linked across StatsBomb, Wyscout, and 2022–2026 scraped sources via `player_id_crosswalk.parquet`). In runtime tensors, Module B assembles `player_cond ∈ R^{B × 22 × 128}` with exact partition `64 (z_p) + 24 (a_eff) + 16 (r_role) + 10 (tau_team) + 5 (phi_instr) + 6 (s_pos) + 3 (s_stat) = 128` for Tokens `0..21`, while routing `gk_attributes_8d` + `finishing_log_odds` to Module B off-ball/role priors, Module D's `PSxG`/finishing log-odds residuals, and Tokens `22..23` (`active_gk_attributes_6d`) of `M_t ∈ R^{B × 28 × 768}`.
3. **Canonical `PlayerRole` Taxonomy (`22` Roles Across All Modules):**
   - All modules standardize on Module A's **22 canonical `PlayerRole` enums (`0..21`)**: `SweeperKeeper`, `ShotStopperGK`, `BallPlayingCB`, `StopperCB`, `WideCenterBack`, `InvertedFullback`, `OverlappingFullback`, `DefensiveFullback`, `CompleteWingBack`, `DeepLyingPlaymaker`, `DestroyerDM`, `BoxToBoxMidfielder`, `Mezzala`, `Carrilero`, `AdvancedPlaymaker`, `InvertedWinger`, `TouchlineWinger`, `InsideForward`, `False9`, `PressingForward`, `TargetForward`, and `Poacher`. Pillar 1 exports `role_suitability_scores ∈ [0,1]^22` and `active_slots_roles ∈ [0..21]` directly in `player_profiles_v2.parquet` and `events_gold_v2.parquet`.
4. **100% SSE Transport, Object Storage & .NET 10 Native AOT Safety:**
   - All live streams—match playback (`GET /api/v2/matches/{id}/stream`), Monte Carlo experiment progress (`GET /api/v2/experiments/{id}/stream`), and user notifications (`GET /api/v2/notifications/stream`)—use **SSE (`text/event-stream`)** exclusively (`sse-starlette` in FastAPI; `System.Threading.Channels.Channel<SseMessage>` in .NET 10). **SignalR is completely removed** (`MatchHub`, `NotificationService` `/Notify`, `AddSignalR()`, `@microsoft/signalr`, `SignalRService.ts`, `useSignalRNotifications.ts`, `useSignalREventStream.ts`). Live mid-match user commands go over stateless REST `POST /api/v2/matches/{id}/tactics` → gRPC `ApplyLiveTacticalOverride`.
   - Compressed 22-player spatial tracks (`~350–500 KB/match` Zstd) offload to **Cloudflare R2 / Azure Blob** via `ISpatialTrackStorageService`, keeping PostgreSQL rows lean.
   - All .NET 10 V2 DTOs (`StructuredEventV2Dto`, `SpatialFrameV2Dto`, `TelemetrySnapshotV2Dto`, `CommentaryCueV2Dto`, `MatchConfigurationV2Dto`, `MonteCarloExperimentResultDto`, `NotificationV2Dto`) must be registered in `MatchEventJsonContext` (`[JsonSerializable]`) for reflection-free Native AOT compilation—eliminating V1's anonymous-object serialization in `MatchEventBroadcaster.BroadcastStatisticsAsync`.

---

## 4. Phased Implementation Roadmap (Phase 1 to Phase 5)

```mermaid
flowchart LR
    P1["Phase 1: Data Lakehouse\nStatsBomb + Wyscout + 2022-2026 Scraping\nCrosswalk + DuckDB/Parquet + Profiler"]
    P2["Phase 2: Hybrid Core & Physics\nModule A Schema + Module B 22-Player Solver\nModule C1 FSM + Module D GPU MLPs + OOD"]
    P3["Phase 3: Base-112M Training\nColab A100 Stage 1 & 2 Debiasing\nStage 3 Calib + Dual Export (CUDA / INT8 ONNX)"]
    P4["Phase 4: Backend & Module E\ngRPC V2 + ComputeBackendRouter + Push RMQ\n.NET 10 Native AOT 100% SSE + R2 + Dual LLM"]
    P5["Phase 5: Frontend & Hardening\n60 FPS 22-Player 3D + 120x80 2D Pitch\nCounterfactual Workbench + Caddy + E2E QA"]

    P1 -->|"HF 16k Shards\n(~3.5 GB)"| P3
    P1 -->|"player_profiles_v2\n+ tactical_manifold_v2"| P2
    P2 -->|"FSM Masks + Module D\n+ Spatial Solver"| P3
    P2 -->|"Contracts & CRN\nExperiment Engine"| P4
    P3 -->|"Exported Base-112M\n+ ONNX INT8"| P4
    P4 -->|"100% SSE Streams\n+ API V2"| P5
```

| Phase | Core Scope | Primary Artifacts / Targets | Objective Exit Gate |
| :---: | :--- | :--- | :--- |
| **Phase 1** | **Data Lakehouse, 2022–2026 Scrapers & Player Profiler** | `statsbombapi_v2` (4,235 SB + 1,941 Wyscout), `soccerdata` scrapers (`FBref`, `Understat`, `SofaScore`/`FotMob`/`WhoScored`, `Transfermarkt`/`EA FC`), `player_id_crosswalk.parquet`, `events_gold_v2.parquet`, `player_profiles_v2.parquet`, `tactical_manifold_v2.pkl`, `hf_dataset_v2/` (`~3.5 GB`), `sync_postgres_v2.py`. | `statsbombapi_v2.validate` passes (`0` spatial/FSM errors, `0` leakage, `mean(xg_v2)` within `±0.005` of StatsBomb, `VAEP` GD correlation `r > 0.65`). |
| **Phase 2** | **Hybrid Tactical State, FSM & Outcome Physics (A, B, C1, D)** | `MatchConfigurationV2` (Pydantic + AOT DTOs), `TacticalStateEngine` (`[B,22,128]` + `~2M` `SpatialPredictor` + `24×16` Pitch Control), `FootballFSM` + hard masks, `D1..D6` CatBoost teachers + GPU MLPs, 4-tier OOD gate (`GREEN`/`AMBER`/`RED`). | `10,000` rollout stress tests produce `0` illegal restarts, `0` OOB coordinates, and distilled MLP `ECE` drift `< 0.002` vs GBDT teachers. |
| **Phase 3** | **`Base-112M` Transformer Training & Calibration (Colab A100/L4)** | `PixelPitch-EventTF-112M` (`12L, d=768, 12Q/4KV, 16k ctx`), Stage 1 pre-training (`~10.7M+` events), Stage 2 causal IPW + CFG dropout + 2022–2026 tactical fine-tuning, Stage 3 realism gates, Stage 4 `torch.compile`/CUDA Graphs + ONNX INT8 KV-cache export. | Scoreline KL `< 0.03`, `xG/team/match` within `±0.07`, `100%` pass on 6 counterfactual monotonicity tests (`n=100` CRN seeds), `N=100` A100 batch `< 20s`. |
| **Phase 4** | **Module E Backend Integration, 100% SSE & Dual LLM Sidecar** | `simulation.proto` V2, `ComputeBackendRouter` (`remote_gpu` / `local_cuda` / `local_cpu_onnx`), FastAPI `/v2/*` (`sse-starlette`), Dual `CommentaryProvider` (`local_3b` + `flash_api` + verifier + `1.5s` sync), .NET 10 V2 controllers (`MatchesV2`, `Experiments`, `NotificationsStream`), `ISpatialTrackStorageService` (R2/Azure Blob), push RabbitMQ, SignalR removal. | `dotnet build` & `dotnet test` pass with zero reflection on SSE paths; end-to-end SSE hop `< 50ms`; LLM verifier catches `100%` of injected score/player mismatches. |
| **Phase 5** | **Next.js 16 22-Player Pitch, Counterfactual Lab & Hardening** | Pure SSE hooks (`useMatchStreamV2`, `useNotificationsSse`), `@microsoft/signalr` removal, R3F `InstancedMesh` 22-player 60 FPS `InterpBuffer`, `120×80` `FootballPitch.tsx` + Voronoi/`xPass`/360 overlays, Counterfactual Experimentation Workbench, Caddy `flush_interval -1`. | Full Docker stack passes E2E live match SSE + Monte Carlo + notification SSE smoke tests at sustained `60 FPS` browser rendering. |

---

## 5. Google Colab A100 / L4 Training & Serving Workflow

Building directly on the existing `uv` + Drive setup in [`simulation-engine/colab_simulation_engine.ipynb`](file:///D:/programming/GitHub/PixelPitchAI/simulation-engine/colab_simulation_engine.ipynb):

1. **Environment Bootstrap (Preserving Colab Native CUDA):**
   - Mount Google Drive (`/content/drive/MyDrive/PixelPitchAI-v2/`) and extract code/artifacts to `/content/simulation-engine` on fast local NVMe.
   - Install dependencies with `uv pip install --system -r pyproject.toml --no-sources` (ignoring the local CPU-only `pytorch-cpu` index in `pyproject.toml` so Colab's CUDA PyTorch + FlashAttention-2 remain intact).
2. **Stage 0 — Lakehouse & Recent-Season Scraping Build (`~15–20 min` CPU/NVMe, cached once to Drive):**
   - Shallow-clone `hudl/open-data` (`--depth 1`), ingest Wyscout Open + cached 2022–2026 scraped feeds (`FBref`, `Understat`, `SofaScore`/`FotMob`/`WhoScored`, `Transfermarkt`/`EA FC` via `player_id_crosswalk.parquet`), and run DuckDB ETL → `events_gold_v2.parquet`, `player_profiles_v2.parquet`, `tactical_manifold_v2.pkl`, `xt_grid_24x16.npy`, and `hf_dataset_v2/` (`~3.5 GB` total).
3. **Module D & Spatial Predictor Training (`~25 min` on A100/L4):**
   - Train 5-fold OOF CatBoost/LightGBM teachers (`D1..D6`), export SHAP/calibration curves, distill to GPU residual MLPs (`~0.15M` params each), and train Module B's `~2M` `SpatialPredictor` on StatsBomb 360 + SkillCorner windows.
4. **Module C Transformer Training (`PixelPitch-EventTF-112M`, `bf16`):**
   - **VRAM Budget (`Base-112M`, `batch=2 × 16,384` varlen packed = `32,768` steps/micro-batch, `grad_accum=4`)**: `~1.8 GB` params/optimizer + `~7–11 GB` activations → fits comfortably on **Colab L4 (24 GB)** and saturates Tensor Cores on **Colab A100 (40/80 GB)**.
   - **Stage 1 Pre-Training (`15` epochs, `~160M` event steps):** `~6–9 hours` on **A100** (`~18–24 hours` on L4) with Drive checkpointing every epoch.
   - **Stage 2 Causal Debiasing (`3` epochs):** Fine-tune with CFG conditioning dropout (`p_tactic=0.15`, `p_player=0.10`), propensity weights (`propensity_tactic_given_elo.pkl`), and 2022–2026 tactical/halftime/red-card upweighting (`~1.5–2 hours` on A100).
5. **Stage 3/4 Calibration, Export & Serving (`ComputeBackendRouter`):**
   - Run automated `500`-match realism & counterfactual monotonicity suite (`03_...` §4.3).
   - Export `PixelPitch-EventTF-112M.safetensors` + ONNX INT8 KV-cache bundle + `module_d_mlp.pt` + `player_profiles_v2.parquet` to Drive / HuggingFace Hub, mount read-only into local/cloud `simulation-engine` containers (`./simulation-engine/models_v2:/app/models_v2:ro` per `CLAUDE.md`), and optionally expose the Colab/Cloud A100 endpoint as `REMOTE_GPU_WORKER_URL` for `ComputeBackendRouter`.

---

## 6. Locked Architectural Decisions

All 6 cross-cutting architecture choices have been finalized and locked across Pillar 1 and Modules A–E:

1. **Transformer Scale — `Base-112M` (`PixelPitch-EventTF-112M`):**
   - Standardized on `12L, d_model=768, 12Q/4KV GQA, RoPE theta=500k, SwiGLU (d_ff=2048), 16,384-step context` + `2L (d=512)` intra-event causal micro-decoder (`26.4 MB` KV cache/match in `bf16`), balancing tactical expressiveness, Colab A100 training speed (`~6–9h`), and `B=128–256` GPU Monte Carlo throughput (`~10–18s` per `100` matches).
2. **Supporting LLM Commentary — Dual `CommentaryProvider` (Local `3B` + Flash API + A/B Telemetry):**
   - Both **`LocalSidecarCommentaryProvider`** (`Qwen2.5-3B-Instruct` / `Llama-3.2-3B` INT4 via `llama.cpp`/vLLM) and **`FlashApiCommentaryProvider`** (external Flash API e.g. Gemini Flash) are implemented behind `CommentaryProvider`, with runtime `ab_split` switching, deterministic score/roster verification, `<1ms` Tier-1 micro-template fallback (`600ms` SLA), and per-cue latency/quality telemetry.
3. **Three-Tier Compute Routing (`ComputeBackendRouter`):**
   - Seamless runtime routing across **`remote_gpu`** (Colab/Cloud A100/L4 for training & `100–1,000` match Monte Carlo experiments), **`local_cuda`** (`torch.compile`/FP16 for `25–100` local batches & GPU live streams), and **`local_cpu_onnx`** (INT8 KV-cache ONNX for single live matches `~4–6 ms/event` & small `10–25` run local counterfactuals).
4. **Expanded Multi-Source Training & 2022–2026 Web Scraping Pipeline:**
   - Locked **`StatsBomb Open (4,235)` + `Wyscout Open (1,941)`** pre-training corpus, augmented with a dedicated **`2022–2026` Multi-Source Scraping & Enrichment Pipeline** (`FBref`, `Understat`, `SofaScore`/`FotMob`/`WhoScored`, `Transfermarkt`/`EA FC`) resolved via `player_id_crosswalk.parquet` (`~3.5 GB` total Gold dataset footprint).
5. **Spatial Track Persistence — Cloudflare R2 / Azure Blob (`ISpatialTrackStorageService`):**
   - Zstd-compressed `5 Hz` 22-player spatial track blobs (`~350–500 KB/match`) offload to **Cloudflare R2** (primary S3-compatible zero-egress store) or **Azure Blob Storage** (extending `AzureBlobStorageService.cs`) behind `ISpatialTrackStorageService`, keeping only object keys and match summaries in PostgreSQL.
6. **100% SSE Migration Across All Real-Time Channels (Zero SignalR):**
   - All live match (`GET /api/v2/matches/{id}/stream`), Monte Carlo progress (`GET /api/v2/experiments/{id}/stream`), and user notification (`GET /api/v2/notifications/stream`) feeds run exclusively over **SSE** (`sse-starlette` + .NET 10 `Channel<SseMessage>`). SignalR (`MatchHub`, `/Notify`, `AddSignalR()`, `@microsoft/signalr`, `SignalRService.ts`, `useSignalRNotifications.ts`) is removed completely from backend and frontend.
