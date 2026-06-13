#!/bin/bash
set -e

# Zero-Downtime Blue/Green Deployment Script
# Paths are relative to the repository root directory

UPSTREAM_CONF="caddy/api_upstream.conf"

# Ensure the upstream file exists, default to blue if missing
if [ ! -f "$UPSTREAM_CONF" ]; then
  mkdir -p caddy
  echo "reverse_proxy footex-api-blue:8080" > "$UPSTREAM_CONF"
fi

# Detect currently active container
if grep -q "footex-api-green" "$UPSTREAM_CONF"; then
  ACTIVE="green"
  INACTIVE="blue"
else
  ACTIVE="blue"
  INACTIVE="green"
fi

echo "🚀 Deploying to GVM. Active environment: $ACTIVE. Target environment: $INACTIVE."

# 1. Pull the new image for the inactive service
echo "📥 Pulling image for footex-api-$INACTIVE..."
docker compose pull footex-api-$INACTIVE

# 2. Start the inactive container
echo "🔄 Starting footex-api-$INACTIVE..."
docker compose up -d footex-api-$INACTIVE

# 3. Poll the new container's health check (exec curl inside container)
echo "⏳ Checking health of footex-api-$INACTIVE..."
HEALTHY=false
for i in $(seq 1 30); do
  if docker exec footex-api-$INACTIVE curl -sf http://localhost:8080/api/health > /dev/null; then
    echo "✅ footex-api-$INACTIVE is healthy!"
    HEALTHY=true
    break
  fi
  echo "⏳ Attempt $i/30: Container is not ready yet. Waiting 5 seconds..."
  sleep 5
done

if [ "$HEALTHY" = false ]; then
  echo "❌ Deployment failed: footex-api-$INACTIVE did not pass health checks."
  echo "🧹 Rolling back (stopping failed container)..."
  docker compose stop footex-api-$INACTIVE
  exit 1
fi

# 4. Update Caddy upstream config
echo "🔄 Switching Caddy traffic to footex-api-$INACTIVE..."
echo "reverse_proxy footex-api-$INACTIVE:8080" > "$UPSTREAM_CONF"

# 5. Hot-reload Caddy
echo "⚡ Reloading Caddy configuration..."
docker compose exec -w /etc/caddy caddy caddy reload

# 6. Stop the old container
echo "🛑 Stopping the old active container (footex-api-$ACTIVE)..."
sleep 5 # Allow a small buffer for ongoing requests to drain
docker compose stop footex-api-$ACTIVE

echo "🎉 Zero-downtime deployment to footex-api-$INACTIVE completed successfully!"
