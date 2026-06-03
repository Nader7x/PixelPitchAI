# Football Match Simulation API Docker Image
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
ENV PATH="/app/.venv/bin:$PATH" \
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
RUN mkdir -p HeaderLines InputTokens SimulatedMatches SimulationStatus

# Expose port
EXPOSE 8000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:8000/health', timeout=10)" || exit 1

# Run the application
CMD ["uvicorn", "api.main:app", "--host", "0.0.0.0", "--port", "8000"]