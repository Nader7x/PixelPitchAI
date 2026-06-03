# ⚽ Football Match Simulation Engine

A high-performance text-generation and event-parsing service built with **FastAPI (Python 3.12)**. This service simulates football matches play-by-play, parses match descriptions into structured events, and streams them in real-time.

---

## 🚀 Key Features

* **Real-time Event Streaming**: Generates match commentary and streams parsed match events dynamically to RabbitMQ.
* **Hybrid Inference Execution**:
  * **CPU Mode (Local Dev)**: Leverages **INT8 dynamically-quantized ONNX Runtime** with physical core thread pinning, yielding **53.3 tokens/second** on standard laptop CPUs (a **2.67x speedup** over raw PyTorch CPU execution).
  * **GPU Mode (Production)**: Seamlessly falls back to native **PyTorch CUDA** (e.g., RTX 3090) when GPU access is detected.
* **Smart Loop Sizing**: Iterates text generation dynamically to avoid the GPT-2 context window limit (1024 tokens) while maintaining high Key-Value (KV) cache hit rates.

---

## 🛠️ Technology Stack

* **Web Framework**: FastAPI & Uvicorn
* **AI & Inference**: ONNX Runtime, Hugging Face Optimum, PyTorch, XGBoost
* **Process Manager**: `uv` (managed virtual environments and execution)
* **Message Broker**: RabbitMQ (via `pika`)

---

## 📂 Project Structure

* [api/core/parser.py](file:///d:/programming/GitHub/Footex/simulation-engine/api/core/parser.py) — Parsers match commentaries into structured event JSON schemas.
* [api/core/xgboost_class.py](file:///d:/programming/GitHub/Footex/simulation-engine/api/core/xgboost_class.py) — Prepares match stats variables and injects them as starting team context.
* [api/services/optimized_simulation_service.py](file:///d:/programming/GitHub/Footex/simulation-engine/api/services/optimized_simulation_service.py) — The core service managing the optimized CPU/GPU generation loop.
* [scripts/](file:///d:/programming/GitHub/Footex/simulation-engine/scripts) — Utility scripts, including model exporters, quantizers, and benchmark suites.

---

## ⚙️ Quick Start

This project requires **uv** for python packaging management.

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

### 3. Run the Service
Ensure RabbitMQ is running (e.g. `docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3.12-management-alpine`), then run:
```bash
uv run uvicorn api.main:app --host 127.0.0.1 --port 8000 --reload
```

---

## 🔌 API Endpoints

### `POST /generate`
Generates raw commentary text based on an initial prompt.
* **Payload**:
  ```json
  {
    "input_text": "Barcelona vs Real Madrid...",
    "num_tokens_to_generate": 400,
    "temperature": 0.7,
    "top_p": 0.9,
    "top_k": 50
  }
  ```

### `POST /startmatch`
Generates a match simulation and publishes parsed events to RabbitMQ in real-time.
* **Payload**:
  ```json
  {
    "home_team": "Barcelona",
    "away_team": "Real Madrid",
    "home_team_season": "2020/2021",
    "away_team_season": "2020/2021",
    "num_tokens_to_generate": 1000
  }
  ```

---

## 📈 Performance Benchmarking

To measure and compare the throughput of different CPU execution options:
```bash
# Benchmark PyTorch vs FP32 ONNX vs Quantized INT8 ONNX
uv run python scripts/benchmark_onnx_vs_pytorch.py

# Run service-level comparison
uv run python scripts/performance_comparison.py
```
