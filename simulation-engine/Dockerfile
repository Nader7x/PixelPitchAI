# Football Match Simulation API (V1 + V2 Hybrid Engine & gRPC) Docker Image
FROM python:3.12-slim
COPY --from=ghcr.io/astral-sh/uv:latest /uv /uvx /bin/

# 1. Create the non-root user FIRST
RUN useradd -m appuser

# 2. Set working directory
WORKDIR /app

# 3. Give the new user ownership of the empty /app folder
RUN chown appuser:appuser /app

# 4. Switch to the non-root user BEFORE doing any heavy lifting
USER appuser

# 5. Set environment variables (Added early so PATH is ready for uv)
# V2 checkpoints live in MODEL_V2_DIR; V1 paths are preserved for backward compatibility
ENV PATH="/app/.venv/bin:$PATH" \
    MODEL_V2_DIR=/app/models_v2 \
    MODEL_PATH=./gpt2-football-finetuned \
    XGB_MODEL_PATH=tuned_xgboost_model.json \
    HEADERLINES_DIR=./HeaderLines \
    INPUTTOKENS_DIR=./InputTokens \
    SIMMATCHES_DIR=./SimulatedMatches \
    STATUS_DIR=./SimulationStatus

# 6. Copy python package definitions with instant ownership
COPY --chown=appuser:appuser pyproject.toml uv.lock ./

# 7. Compile virtual environment (runs as appuser, so no chown needed later!)
RUN uv sync --frozen --no-install-project -v

# 8. Copy application code with instant ownership
COPY --chown=appuser:appuser . .

# 9. Create necessary directories natively as the appuser
RUN mkdir -p HeaderLines InputTokens SimulatedMatches SimulationStatus models_v2

# Expose HTTP (FastAPI) and gRPC (SimulationServiceV2) ports
EXPOSE 8000 50051

# Health check (V2 readiness endpoint; boots cleanly even in V2-only mode without V1 weights)
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:8000/v2/health', timeout=10)" || exit 1

# Run the application (starts FastAPI on :8000 and gRPC SimulationServiceV2 on :50051)
CMD ["uvicorn", "api.main:app", "--host", "0.0.0.0", "--port", "8000"]