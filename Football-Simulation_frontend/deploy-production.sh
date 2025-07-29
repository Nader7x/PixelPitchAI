#!/bin/bash
# Linux/MacOS production deployment script

set -e

DOMAIN=""
EMAIL=""
SKIP_SSL=false
MONITORING=false
WITH_REDIS=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -d|--domain)
            DOMAIN="$2"
            shift 2
            ;;
        -e|--email)
            EMAIL="$2"
            shift 2
            ;;
        --skip-ssl)
            SKIP_SSL=true
            shift
            ;;
        --monitoring)
            MONITORING=true
            shift
            ;;
        --with-redis)
            WITH_REDIS=true
            shift
            ;;
        *)
            echo "Unknown option $1"
            exit 1
            ;;
    esac
done

echo "🚀 Football Simulation - Production Deployment"
echo "============================================="
echo

# Check prerequisites
echo "🔍 Checking prerequisites..."

if ! command -v docker &> /dev/null; then
    echo "❌ Docker not found. Please install Docker first."
    exit 1
fi
echo "✅ Docker is available"

if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose not found. Please install Docker Compose."
    exit 1
fi
echo "✅ Docker Compose is available"

# Environment setup
echo
echo "⚙️  Setting up environment..."

if [ ! -f ".env.production" ]; then
    echo "Creating .env.production from template..."
    cp .env.production.example .env.production
    
    if [ -n "$DOMAIN" ] && [ -n "$EMAIL" ]; then
        echo "Updating environment with provided domain and email..."
        sed -i "s/DOMAIN_NAME=yourdomain.com/DOMAIN_NAME=$DOMAIN/" .env.production
        sed -i "s/SSL_EMAIL=admin@yourdomain.com/SSL_EMAIL=$EMAIL/" .env.production
        sed -i "s|NEXTAUTH_URL=https://yourdomain.com|NEXTAUTH_URL=https://$DOMAIN|" .env.production
        echo "✅ Environment configured for $DOMAIN"
    else
        echo "⚠️  Please edit .env.production with your actual values!"
        echo "   Required: DOMAIN_NAME, SSL_EMAIL, NEXTAUTH_SECRET, etc."
    fi
else
    echo "✅ .env.production already exists"
fi

# SSL setup
if [ "$SKIP_SSL" = false ]; then
    echo
    echo "🔒 Setting up SSL certificates..."
    
    if [ ! -f "ssl/cert.pem" ] || [ ! -f "ssl/key.pem" ]; then
        if [ -n "$DOMAIN" ] && [ -n "$EMAIL" ]; then
            echo "Setting up Let's Encrypt certificates..."
            ./setup-ssl.sh --lets-encrypt --domain "$DOMAIN" --email "$EMAIL"
        else
            echo "⚠️  No SSL certificates found and no domain provided."
            echo "Setting up self-signed certificate for testing..."
            ./setup-ssl.sh --self-signed --domain "localhost"
        fi
    else
        echo "✅ SSL certificates found"
    fi
fi

# Build production image
echo
echo "🏗️  Building production image..."
docker build --target production -t football-simulation:prod .
echo "✅ Production image built successfully"

# Determine compose profiles
PROFILES=""
if [ "$MONITORING" = true ]; then
    PROFILES="$PROFILES --profile monitoring"
    echo "📊 Monitoring enabled"
fi
if [ "$WITH_REDIS" = true ]; then
    PROFILES="$PROFILES --profile with-redis"
    echo "🗃️  Redis caching enabled"
fi

# Start production deployment
echo
echo "🚀 Starting production deployment..."

docker-compose -f docker-compose.prod.yml $PROFILES up -d

echo "✅ Production deployment started successfully!"

# Wait for services to start
echo
echo "⏳ Waiting for services to start..."
sleep 10

# Health check
echo
echo "🏥 Performing health checks..."

HEALTH_URL="http://localhost/health"
if [ -f "ssl/cert.pem" ]; then
    HEALTH_URL="https://localhost/health"
fi

if curl -f "$HEALTH_URL" >/dev/null 2>&1; then
    echo "✅ Application health check passed"
else
    echo "⚠️  Health check failed. Application may still be starting..."
    echo "   Check manually: $HEALTH_URL"
fi

# Show deployment summary
echo
echo "🎉 Deployment Summary"
echo "==================="

ACTUAL_DOMAIN=${DOMAIN:-localhost}
PROTOCOL="http"
if [ -f "ssl/cert.pem" ]; then
    PROTOCOL="https"
fi

echo "🌐 Application URL: $PROTOCOL://$ACTUAL_DOMAIN"
echo "🏥 Health Check: $PROTOCOL://$ACTUAL_DOMAIN/health"

if [ "$MONITORING" = true ]; then
    echo "📊 Prometheus: http://$ACTUAL_DOMAIN:9090"
fi

echo
echo "📋 Useful Commands:"
echo "  View logs: docker-compose -f docker-compose.prod.yml logs -f"
echo "  Stop deployment: docker-compose -f docker-compose.prod.yml down"
echo "  Check status: docker ps"

echo
echo "🚀 Production deployment completed!"
