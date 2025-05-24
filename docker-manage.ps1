# Docker Management Script for Footex Project
# Usage: .\docker-manage.ps1 [command]

param(
    [Parameter(Position=0)]
    [string]$Command = "help"
)

function Show-Help {
    Write-Host "Footex Docker Management Script" -ForegroundColor Green
    Write-Host "================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Available commands:" -ForegroundColor Yellow
    Write-Host "  dev-up       - Start development environment"
    Write-Host "  dev-down     - Stop development environment"
    Write-Host "  dev-rebuild  - Rebuild and start development environment"
    Write-Host "  dev-logs     - Show development logs"
    Write-Host ""
    Write-Host "  prod-up      - Start production environment"
    Write-Host "  prod-down    - Stop production environment"
    Write-Host "  prod-rebuild - Rebuild and start production environment"
    Write-Host "  prod-logs    - Show production logs"
    Write-Host ""
    Write-Host "  db-only      - Start only database and RabbitMQ"
    Write-Host "  clean        - Remove all containers and volumes"
    Write-Host "  setup        - Initial setup (copy env files)"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Cyan
    Write-Host "  .\docker-manage.ps1 dev-up"
    Write-Host "  .\docker-manage.ps1 prod-rebuild"
    Write-Host "  .\docker-manage.ps1 clean"
}

function Setup-Environment {
    Write-Host "Setting up environment files..." -ForegroundColor Yellow
    
    if (-not (Test-Path ".env")) {
        Copy-Item ".env.example" ".env"
        Write-Host "Created .env file from .env.example" -ForegroundColor Green
        Write-Host "Please edit .env file with your actual values" -ForegroundColor Yellow
    }
    
    if (-not (Test-Path ".env.dev")) {
        Copy-Item ".env.dev.example" ".env.dev"
        Write-Host "Created .env.dev file from .env.dev.example" -ForegroundColor Green
    }
    
    Write-Host "Environment setup complete!" -ForegroundColor Green
}

function Start-Development {
    Write-Host "Starting development environment..." -ForegroundColor Yellow
    docker-compose -f docker-compose.dev.yml --env-file .env.dev up -d
    Write-Host "Development environment started!" -ForegroundColor Green
    Write-Host "API: http://localhost:5025" -ForegroundColor Cyan
    Write-Host "RabbitMQ Management: http://localhost:15672 (guest/guest)" -ForegroundColor Cyan
}

function Stop-Development {
    Write-Host "Stopping development environment..." -ForegroundColor Yellow
    docker-compose -f docker-compose.dev.yml down
    Write-Host "Development environment stopped!" -ForegroundColor Green
}

function Rebuild-Development {
    Write-Host "Rebuilding development environment..." -ForegroundColor Yellow
    docker-compose -f docker-compose.dev.yml down
    docker-compose -f docker-compose.dev.yml --env-file .env.dev up --build -d
    Write-Host "Development environment rebuilt and started!" -ForegroundColor Green
}

function Show-Development-Logs {
    docker-compose -f docker-compose.dev.yml logs -f
}

function Start-Production {
    Write-Host "Starting production environment..." -ForegroundColor Yellow
    docker-compose --env-file .env up -d
    Write-Host "Production environment started!" -ForegroundColor Green
    Write-Host "API: http://localhost:8080" -ForegroundColor Cyan
    Write-Host "RabbitMQ Management: http://localhost:15672" -ForegroundColor Cyan
}

function Stop-Production {
    Write-Host "Stopping production environment..." -ForegroundColor Yellow
    docker-compose down
    Write-Host "Production environment stopped!" -ForegroundColor Green
}

function Rebuild-Production {
    Write-Host "Rebuilding production environment..." -ForegroundColor Yellow
    docker-compose down
    docker-compose --env-file .env up --build -d
    Write-Host "Production environment rebuilt and started!" -ForegroundColor Green
}

function Show-Production-Logs {
    docker-compose logs -f
}

function Start-DatabaseOnly {
    Write-Host "Starting database and RabbitMQ only..." -ForegroundColor Yellow
    docker-compose -f docker-compose.dev.yml --env-file .env.dev up -d postgres rabbitmq
    Write-Host "Database and RabbitMQ started!" -ForegroundColor Green
}

function Clean-All {
    Write-Host "Cleaning up all containers and volumes..." -ForegroundColor Yellow
    docker-compose -f docker-compose.dev.yml down -v
    docker-compose down -v
    docker system prune -f
    Write-Host "Cleanup complete!" -ForegroundColor Green
}

# Main command execution
switch ($Command.ToLower()) {
    "help" { Show-Help }
    "setup" { Setup-Environment }
    "dev-up" { Start-Development }
    "dev-down" { Stop-Development }
    "dev-rebuild" { Rebuild-Development }
    "dev-logs" { Show-Development-Logs }
    "prod-up" { Start-Production }
    "prod-down" { Stop-Production }
    "prod-rebuild" { Rebuild-Production }
    "prod-logs" { Show-Production-Logs }
    "db-only" { Start-DatabaseOnly }
    "clean" { Clean-All }
    default { 
        Write-Host "Unknown command: $Command" -ForegroundColor Red
        Show-Help 
    }
}
