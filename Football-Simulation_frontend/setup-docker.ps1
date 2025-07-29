# Football Simulation - Docker Setup Script
# Run this script to set up Docker environment for the first time

Write-Host "🏈 Football Simulation - Docker Setup" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
Write-Host ""

# Check if Docker is installed
Write-Host "Checking Docker installation..." -ForegroundColor Yellow
try {
    $dockerVersion = docker --version
    Write-Host "✅ Docker found: $dockerVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker not found. Please install Docker Desktop first." -ForegroundColor Red
    Write-Host "Download from: https://www.docker.com/products/docker-desktop/" -ForegroundColor Yellow
    exit 1
}

# Check if Docker Compose is available
Write-Host "Checking Docker Compose..." -ForegroundColor Yellow
try {
    $composeVersion = docker-compose --version
    Write-Host "✅ Docker Compose found: $composeVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker Compose not found. Please update Docker Desktop." -ForegroundColor Red
    exit 1
}

# Check if Docker is running
Write-Host "Checking Docker daemon..." -ForegroundColor Yellow
try {
    docker ps | Out-Null
    Write-Host "✅ Docker daemon is running" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker daemon is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🔧 Setting up environment..." -ForegroundColor Cyan

# Create environment file if it doesn't exist
if (-not (Test-Path ".env.local")) {
    Write-Host "Creating .env.local from template..." -ForegroundColor Yellow
    Copy-Item ".env.example" ".env.local"
    Write-Host "✅ Created .env.local - please review and update the values" -ForegroundColor Green
} else {
    Write-Host "✅ .env.local already exists" -ForegroundColor Green
}

Write-Host ""
Write-Host "🚀 Docker setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Review and update .env.local with your configuration" -ForegroundColor White
Write-Host "2. For development: pnpm run docker:dev" -ForegroundColor White
Write-Host "3. For production: pnpm run docker:prod" -ForegroundColor White
Write-Host ""
Write-Host "Available commands:" -ForegroundColor Cyan
Write-Host "  pnpm run docker:dev        - Start development environment" -ForegroundColor White
Write-Host "  pnpm run docker:dev:bg     - Start development in background" -ForegroundColor White
Write-Host "  pnpm run docker:prod       - Start production environment" -ForegroundColor White
Write-Host "  pnpm run docker:prod:bg    - Start production in background" -ForegroundColor White
Write-Host "  pnpm run docker:logs:dev   - View development logs" -ForegroundColor White
Write-Host "  pnpm run docker:logs:prod  - View production logs" -ForegroundColor White
Write-Host "  pnpm run docker:clean      - Clean Docker resources" -ForegroundColor White
Write-Host ""
Write-Host "Or use the PowerShell module:" -ForegroundColor Cyan
Write-Host "  . .\docker-scripts.ps1     - Load Docker functions" -ForegroundColor White
Write-Host "  Start-Dev                   - Start development environment" -ForegroundColor White
Write-Host "  Start-Prod                  - Start production environment" -ForegroundColor White
Write-Host ""
Write-Host "Documentation: See DOCKER_DEPLOYMENT_GUIDE.md for detailed instructions" -ForegroundColor Yellow
Write-Host ""

# Ask if user wants to start development environment
$response = Read-Host "Would you like to start the development environment now? (y/N)"
if ($response -eq "y" -or $response -eq "Y") {
    Write-Host ""
    Write-Host "🚀 Starting development environment..." -ForegroundColor Green
    Write-Host "This will build the Docker image and start the containers..." -ForegroundColor Yellow
    Write-Host ""
    
    try {
        & pnpm run docker:dev
    } catch {
        Write-Host "❌ Failed to start development environment" -ForegroundColor Red
        Write-Host "Please check the error messages above and ensure:" -ForegroundColor Yellow
        Write-Host "- Docker Desktop is running" -ForegroundColor White
        Write-Host "- No other services are using port 3000" -ForegroundColor White
        Write-Host "- .env.local is properly configured" -ForegroundColor White
    }
} else {
    Write-Host "Setup complete! Run 'pnpm run docker:dev' when ready to start." -ForegroundColor Green
}
