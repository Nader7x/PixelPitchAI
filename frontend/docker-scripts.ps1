# Docker PowerShell Scripts for Windows

# Build development image
function Build-Dev {
    docker build --target development -t football-simulation:dev .
}

# Build production image
function Build-Prod {
    docker build --target production -t football-simulation:prod .
}

# Run development environment
function Start-Dev {
    docker-compose up --build
}

# Run development environment in background
function Start-DevBackground {
    docker-compose up --build -d
}

# Stop development environment
function Stop-Dev {
    docker-compose down
}

# Run production environment
function Start-Prod {
    docker-compose -f docker-compose.prod.yml up --build
}

# Run production environment in background
function Start-ProdBackground {
    docker-compose -f docker-compose.prod.yml up --build -d
}

# Stop production environment
function Stop-Prod {
    docker-compose -f docker-compose.prod.yml down
}

# View logs
function Show-DevLogs {
    docker-compose logs -f
}

function Show-ProdLogs {
    docker-compose -f docker-compose.prod.yml logs -f
}

# Clean up Docker resources
function Clean-Docker {
    docker system prune -f
    docker volume prune -f
}

# Remove all containers and images
function Clean-All {
    docker-compose down --rmi all --volumes
    docker-compose -f docker-compose.prod.yml down --rmi all --volumes
}

# Export functions
Export-ModuleMember -Function Build-Dev, Build-Prod, Start-Dev, Start-DevBackground, Stop-Dev, Start-Prod, Start-ProdBackground, Stop-Prod, Show-DevLogs, Show-ProdLogs, Clean-Docker, Clean-All
