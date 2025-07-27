# Football Match Simulation API Docker Image
FROM python:3.12-slim

# Set working directory
WORKDIR /app

# Install system dependencies
RUN apt-get update && apt-get install -y \
    gcc \
    g++ \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy requirements and install Python dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy application code
COPY . .

# Create necessary directories
RUN mkdir -p HeaderLines InputTokens SimulatedMatches SimulationStatus

# Set environment variables
ENV MODEL_PATH=./gpt2-football-finetuned
ENV XGB_MODEL_PATH=tuned_xgboost_model.json
ENV HEADERLINES_DIR=./HeaderLines
ENV INPUTTOKENS_DIR=./InputTokens
ENV SIMMATCHES_DIR=./SimulatedMatches
ENV STATUS_DIR=./SimulationStatus

# Expose port
EXPOSE 8000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8000/health || exit 1

# Run the application
CMD ["uvicorn", "ModelApiEnhanced:app", "--host", "0.0.0.0", "--port", "8000"]