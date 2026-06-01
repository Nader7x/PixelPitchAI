# Football Match Simulation API Docker Image
FROM python:3.12-slim
COPY --from=ghcr.io/astral-sh/uv:latest /uv /uvx /bin/

# Set working directory
WORKDIR /app


# Copy python package definitions
COPY pyproject.toml uv.lock ./

# Compile virtual environment and install dependencies cleanly
RUN uv sync --frozen --no-install-project --no-cache

# Copy application code
COPY . .

# Create necessary directories
RUN mkdir -p HeaderLines InputTokens SimulatedMatches SimulationStatus

# Create a non-root user
RUN useradd -m appuser

# Change ownership of the working directory and necessary folders
RUN chown -R appuser:appuser /app /HeaderLines /InputTokens /SimulatedMatches /SimulationStatus

# Switch to the non-root user
USER appuser

# Set environment variables
ENV PATH="/app/.venv/bin:$PATH"
ENV MODEL_PATH=./gpt2-football-finetuned
ENV XGB_MODEL_PATH=tuned_xgboost_model.json
ENV HEADERLINES_DIR=./HeaderLines
ENV INPUTTOKENS_DIR=./InputTokens
ENV SIMMATCHES_DIR=./SimulatedMatches
ENV STATUS_DIR=./SimulationStatus

# Expose port
EXPOSE 8000

# Health check using native Python standard library (no curl needed)
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:8000/health', timeout=10)" || exit 1

# Run the application
CMD ["python", "run.py"]
