# 🏟️ PixelPitchAI Simulation Engine

A high-performance AI text-generation, event-streaming, and match simulation microservice built on **Python 3.12** managed with **uv**. The engine simulates football matches play-by-play using fine-tuned language models and gradient-boosted trees. It runs concurrently across two server interfaces:
* **FastAPI REST API**: Port 8000 for control, status polling, and webhooks
* **gRPC SimulationService**: Port 50051 for high-throughput streaming

---

## 🚀 Key Features

* **Dual-Server Concurrency**: Concurrently hosts two server interfaces via Python 3.12 `asyncio` within Uvicorn's unified lifespan manager:
  * FastAPI REST control plane: Port 8000
  * gRPC streaming server: Port 50051
* **Dual Streaming Ingestion**:
  * **Pipeline A (Durable RabbitMQ Stream)**: Concurrently publishes raw text lines to RabbitMQ topic exchange `match_events` (routing key `match.events`) for asynchronous buffering, zero-allocation parsing, and PostgreSQL batch persistence.
  * **Pipeline B (Direct gRPC Server-Streaming)**: Low-latency memory-to-memory streaming via `SimulationService.StartMatchSimulationStream`, yielding `MatchEventRaw` messages directly to downstream consumers for real-time spectator delivery.
* **Hybrid Inference Execution**:
  * **CPU Mode (Local Dev)**: Leverages **INT8 dynamically-quantized ONNX Runtime** with physical core thread pinning, yielding **53.3 tokens/second** on standard laptop CPUs (a **2.67x speedup** over raw PyTorch CPU execution).
  * **GPU Mode (Production)**: Seamlessly falls back to native **PyTorch CUDA** (e.g., RTX 3090) when GPU access is detected.
* **Smart Loop Sizing**: Iterates text generation dynamically to avoid the GPT-2 context window limit (1024 tokens) while maintaining high Key-Value (KV) cache hit rates.

---

## 🛠️ Technology Stack

* **Runtime & Package Manager**: Python 3.12 managed with `uv` (`pyproject.toml`)
* **Web & RPC Frameworks**:
  * FastAPI REST API (port 8000) & Uvicorn (`asyncio`)
  * gRPC / Protobuf Service (port 50051)
* **AI & Inference**: ONNX Runtime, Hugging Face Optimum, PyTorch, XGBoost
* **Message Broker**: RabbitMQ (`pika`) exchange `match_events` (routing key `match.events`)

---

## 📂 Project Structure

* [api/core/parser.py](./api/core/parser.py) — Parses match commentaries into structured event JSON schemas and publishes to RabbitMQ.
* [api/core/xgboost_class.py](./api/core/xgboost_class.py) — Prepares match stats variables and injects them as starting team context.
* [api/services/optimized_simulation_service.py](./api/services/optimized_simulation_service.py) — The core service managing the optimized CPU/GPU generation loop.
* [scripts/](./scripts) — Utility scripts including model exporters, quantizers, and benchmark suites.

---

## ⚙️ Quick Start

This project requires **uv** for Python package management.

### 1. Install Dependencies
```bash
uv sync --frozen
```

### 2. Export & Quantize Model (CPU Only)
To run with high-performance CPU inference:
```bash
# Export standard model weights to ONNX format
uv run python scripts/export_to_onnx.py

# Quantize the ONNX weights to INT8
uv run python scripts/quantize_onnx.py
```

### 3. Run the Dual-Server Microservice
Ensure RabbitMQ is running (e.g. `docker-compose up -d rabbitmq`), then launch the dual-server daemon concurrently via Uvicorn:
```bash
uv run python run.py
```
This concurrently boots:
* FastAPI REST API on port 8000
* gRPC SimulationService on port 50051

---

## 📡 gRPC Simulation Service (`port 50051`)

The gRPC interface is defined in `api/protos/simulation.proto` under package `footex.simulation`:

```protobuf
syntax = "proto3";

package footex.simulation;

service SimulationService {
  rpc StartMatchSimulation (SimulateMatchRequest) returns (StartMatchResponse);
  rpc StartMatchSimulationStream (SimulateMatchRequest) returns (stream MatchEventRaw);
  rpc GetHealth (HealthRequest) returns (HealthResponse);
}
```

### RPC Methods
* **`StartMatchSimulation`** (Unary): Asynchronously starts a match simulation in background tasks and returns an initial `StartMatchResponse` containing `simulation_id` and status.
* **`StartMatchSimulationStream`** (Server-Streaming): Streams match events in real time as `MatchEventRaw` messages (Pipeline B) while simultaneously publishing each event to RabbitMQ (Pipeline A).
* **`GetHealth`** (Unary): Returns engine health status, including whether the PyTorch/ONNX model and XGBoost model weights are loaded.

---

## 🔌 FastAPI REST Endpoints (`port 8000`)

### Match Simulation Execution
* **Route**: `/startMatch`
* **Method**: `POST`
Starts an asynchronous match simulation job and returns immediately with a `MatchResponse`.
* **Payload (`MatchRequest`)**:
  ```json
  {
    "home_team_id": 7,
    "away_team_id": 2,
    "home_team_name": "Real Madrid",
    "away_team_name": "Barcelona",
    "home_team_season": "2020/2021",
    "away_team_season": "2020/2021",
    "match_id": 12345,
    "num_tokens_to_generate": 200000,
    "temperature": 0.7,
    "top_p": 0.9,
    "top_k": 50,
    "max_length": 1024
  }
  ```

### Simulation Status Polling
* **Route**: `/simulationStatus/{simulation_id}`
* **Method**: `GET`
Returns the current execution status, progress percentage, and event count of a running or completed simulation.

### Simulation Result Retrieval
* **Route**: `/simulationResult/{simulation_id}`
* **Method**: `GET`
Retrieves the completed match simulation text, metadata, and execution duration.

### Match Webhook Registration
* **Route**: `/simulations/{simulation_id}/webhook`
* **Method**: `POST`
Registers a webhook callback URL (with optional HMAC secret) to be notified upon match simulation completion.

### System Telemetry Stats
* **Route**: `/stats`
* **Method**: `GET`
Returns system resource telemetry including CPU percent, memory percent, disk usage, and CUDA availability.

### Liveness Health Check
* **Route**: `/health`
* **Method**: `GET`
Liveness check returning service status, model loaded state, and version.

---

## 📈 Performance Benchmarking

To measure and compare the throughput of inference execution options:
```bash
# Benchmark model throughput and generation latency
uv run python scripts/benchmark_performance.py

# Run comprehensive service-level comparison
uv run python scripts/performance_comparison.py
```

