# Production Deployment Script for Football Simulation
# This script automates the complete production deployment process

param(
    [string]$Domain = "",
    [string]$Email = "",
    [switch]$SkipSSL = $false,
    [switch]$Monitoring = $false,
    [switch]$WithRedis = $false
)

Write-Host "🚀 Football Simulation - Production Deployment" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host ""

# Check prerequisites
Write-Host "🔍 Checking prerequisites..." -ForegroundColor Yellow

# Check Docker
try {
    docker --version | Out-Null
    Write-Host "✅ Docker is available" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker not found. Please install Docker Desktop." -ForegroundColor Red
    exit 1
}

# Check Docker Compose
try {
    docker-compose --version | Out-Null
    Write-Host "✅ Docker Compose is available" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker Compose not found. Please update Docker." -ForegroundColor Red
    exit 1
}

# Validate configuration
Write-Host ""
Write-Host "🔧 Validating configuration..." -ForegroundColor Yellow
try {
    & .\validate-docker.ps1
} catch {
    Write-Host "⚠️  Validation script not found. Continuing anyway..." -ForegroundColor Yellow
}

# Environment setup
Write-Host ""
Write-Host "⚙️  Setting up environment..." -ForegroundColor Cyan

if (-not (Test-Path ".env.production")) {
    Write-Host "Creating .env.production from template..." -ForegroundColor Yellow
    Copy-Item ".env.production.example" ".env.production"
    
    if ($Domain -and $Email) {
        Write-Host "Updating environment with provided domain and email..." -ForegroundColor Cyan
        $envContent = Get-Content ".env.production" -Raw
        $envContent = $envContent -replace "DOMAIN_NAME=yourdomain.com", "DOMAIN_NAME=$Domain"
        $envContent = $envContent -replace "SSL_EMAIL=admin@yourdomain.com", "SSL_EMAIL=$Email"
        $envContent = $envContent -replace "NEXTAUTH_URL=https://yourdomain.com", "NEXTAUTH_URL=https://$Domain"
        Set-Content ".env.production" $envContent
        Write-Host "✅ Environment configured for $Domain" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Please edit .env.production with your actual values!" -ForegroundColor Yellow
        Write-Host "   Required: DOMAIN_NAME, SSL_EMAIL, NEXTAUTH_SECRET, etc." -ForegroundColor White
        
        $editNow = Read-Host "Would you like to edit .env.production now? (y/N)"
        if ($editNow -eq "y" -or $editNow -eq "Y") {
            notepad ".env.production"
            Write-Host "Press any key when you've finished editing the environment file..."
            $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        }
    }
} else {
    Write-Host "✅ .env.production already exists" -ForegroundColor Green
}

# SSL Certificate setup
if (-not $SkipSSL) {
    Write-Host ""
    Write-Host "🔒 Setting up SSL certificates..." -ForegroundColor Cyan
    
    if (-not (Test-Path "ssl/cert.pem") -or -not (Test-Path "ssl/key.pem")) {
        if ($Domain -and $Email) {
            Write-Host "Setting up Let's Encrypt certificates..." -ForegroundColor Yellow
            try {
                & .\setup-ssl.ps1 -LetsEncrypt -Domain $Domain -Email $Email
            } catch {
                Write-Host "❌ SSL setup failed: $($_.Exception.Message)" -ForegroundColor Red
                Write-Host "You can skip SSL for now and set it up later." -ForegroundColor Yellow
                $skipSSL = Read-Host "Continue without SSL? (y/N)"
                if ($skipSSL -ne "y" -and $skipSSL -ne "Y") {
                    exit 1
                }
            }
        } else {
            Write-Host "⚠️  No SSL certificates found and no domain provided." -ForegroundColor Yellow
            Write-Host "Options:" -ForegroundColor Cyan
            Write-Host "1. Set up Let's Encrypt (recommended for production)" -ForegroundColor White
            Write-Host "2. Set up self-signed certificates (for testing)" -ForegroundColor White
            Write-Host "3. Skip SSL setup (not recommended for production)" -ForegroundColor White
            
            $sslChoice = Read-Host "Choose option (1-3)"
            
            switch ($sslChoice) {
                "1" {
                    $domain = Read-Host "Enter your domain name"
                    $email = Read-Host "Enter your email address"
                    if ($domain -and $email) {
                        & .\setup-ssl.ps1 -LetsEncrypt -Domain $domain -Email $email
                    }
                }
                "2" {
                    $domain = Read-Host "Enter domain name (or 'localhost')"
                    if (-not $domain) { $domain = "localhost" }
                    & .\setup-ssl.ps1 -SelfSigned -Domain $domain
                }
                "3" {
                    Write-Host "⚠️  Continuing without SSL. HTTPS will not be available." -ForegroundColor Yellow
                }
            }
        }
    } else {
        Write-Host "✅ SSL certificates found" -ForegroundColor Green
    }
}

# Build production image
Write-Host ""
Write-Host "🏗️  Building production image..." -ForegroundColor Cyan
try {
    docker build --target production -t football-simulation:prod .
    Write-Host "✅ Production image built successfully" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to build production image: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Determine compose profiles
$profiles = @()
if ($Monitoring) {
    $profiles += "monitoring"
    Write-Host "📊 Monitoring enabled" -ForegroundColor Cyan
}
if ($WithRedis) {
    $profiles += "with-redis"
    Write-Host "🗃️  Redis caching enabled" -ForegroundColor Cyan
}

# Start production deployment
Write-Host ""
Write-Host "🚀 Starting production deployment..." -ForegroundColor Green

$composeCommand = "docker-compose -f docker-compose.prod.yml"
if ($profiles.Count -gt 0) {
    foreach ($profile in $profiles) {
        $composeCommand += " --profile $profile"
    }
}
$composeCommand += " up -d"

try {
    Invoke-Expression $composeCommand
    Write-Host "✅ Production deployment started successfully!" -ForegroundColor Green
} catch {
    Write-Host "❌ Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Check the logs for more details:" -ForegroundColor Yellow
    Write-Host "pnpm run docker:logs:prod" -ForegroundColor Cyan
    exit 1
}

# Wait for services to start
Write-Host ""
Write-Host "⏳ Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Health check
Write-Host ""
Write-Host "🏥 Performing health checks..." -ForegroundColor Cyan

$healthCheckUrl = if (Test-Path "ssl/cert.pem") { "https://localhost/health" } else { "http://localhost/health" }

try {
    $response = Invoke-WebRequest -Uri $healthCheckUrl -UseBasicParsing -TimeoutSec 30
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Application health check passed" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Health check returned status: $($response.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️  Health check failed. Application may still be starting..." -ForegroundColor Yellow
    Write-Host "   You can check manually: $healthCheckUrl" -ForegroundColor White
}

# Show deployment summary
Write-Host ""
Write-Host "🎉 Deployment Summary" -ForegroundColor Green
Write-Host "===================" -ForegroundColor Green

$actualDomain = if ($Domain) { $Domain } else { "localhost" }
$protocol = if (Test-Path "ssl/cert.pem") { "https" } else { "http" }

Write-Host "🌐 Application URL: $protocol://$actualDomain" -ForegroundColor Cyan
Write-Host "🏥 Health Check: $protocol://$actualDomain/health" -ForegroundColor Cyan

if ($Monitoring) {
    Write-Host "📊 Prometheus: http://$actualDomain:9090" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "📋 Useful Commands:" -ForegroundColor Yellow
Write-Host "  View logs: pnpm run docker:logs:prod" -ForegroundColor White
Write-Host "  Stop deployment: pnpm run docker:prod:down" -ForegroundColor White
Write-Host "  Restart deployment: pnpm run docker:prod:bg" -ForegroundColor White
Write-Host "  Check status: docker ps" -ForegroundColor White

if (-not $SkipSSL -and (Test-Path "ssl/cert.pem")) {
    Write-Host ""
    Write-Host "🔒 SSL Configuration:" -ForegroundColor Green
    Write-Host "  HTTPS is enabled and configured" -ForegroundColor White
    Write-Host "  Certificates are located in ssl/ directory" -ForegroundColor White
    Write-Host "  Automatic HTTP to HTTPS redirect is active" -ForegroundColor White
}

Write-Host ""
Write-Host "🎯 Next Steps:" -ForegroundColor Cyan
Write-Host "1. Configure your domain's DNS to point to this server" -ForegroundColor White
Write-Host "2. Test your application thoroughly" -ForegroundColor White
Write-Host "3. Set up monitoring and alerting" -ForegroundColor White
Write-Host "4. Configure automated backups" -ForegroundColor White
Write-Host "5. Review security settings" -ForegroundColor White

Write-Host ""
Write-Host "📚 Documentation:" -ForegroundColor Yellow
Write-Host "  Full hosting guide: HTTPS_HOSTING_GUIDE.md" -ForegroundColor White
Write-Host "  Docker guide: DOCKER_DEPLOYMENT_GUIDE.md" -ForegroundColor White

Write-Host ""
Write-Host "🚀 Production deployment completed!" -ForegroundColor Green
