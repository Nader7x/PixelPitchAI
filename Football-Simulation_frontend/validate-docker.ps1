# Docker Configuration Validation Script

Write-Host "🔍 Validating Docker Configuration..." -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

$errors = @()
$warnings = @()

# Check required files
$requiredFiles = @(
    "Dockerfile",
    "docker-compose.yml", 
    "docker-compose.prod.yml",
    ".dockerignore",
    "package.json",
    "next.config.ts"
)

Write-Host "📁 Checking required files..." -ForegroundColor Yellow
foreach ($file in $requiredFiles) {
    if (Test-Path $file) {
        Write-Host "  ✅ $file" -ForegroundColor Green
    } else {
        Write-Host "  ❌ $file (missing)" -ForegroundColor Red
        $errors += "Missing required file: $file"
    }
}

# Check .env.example
if (Test-Path ".env.example") {
    Write-Host "  ✅ .env.example" -ForegroundColor Green
} else {
    Write-Host "  ⚠️  .env.example (missing - template not available)" -ForegroundColor Yellow
    $warnings += "No .env.example template found"
}

# Check .env.local
if (Test-Path ".env.local") {
    Write-Host "  ✅ .env.local" -ForegroundColor Green
} else {
    Write-Host "  ⚠️  .env.local (missing - run setup-docker.ps1)" -ForegroundColor Yellow
    $warnings += "No .env.local found - run setup-docker.ps1 to create"
}

Write-Host ""

# Validate Dockerfile stages
Write-Host "🐳 Validating Dockerfile..." -ForegroundColor Yellow
$dockerfileContent = Get-Content "Dockerfile" -Raw
$stages = @("base", "deps", "development", "builder", "production")

foreach ($stage in $stages) {
    if ($dockerfileContent -match "FROM .* AS $stage") {
        Write-Host "  ✅ Stage: $stage" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Stage: $stage (missing)" -ForegroundColor Red
        $errors += "Missing Dockerfile stage: $stage"
    }
}

# Check Docker Compose services
Write-Host ""
Write-Host "🔧 Validating Docker Compose..." -ForegroundColor Yellow

# Development compose
if (Test-Path "docker-compose.yml") {
    try {
        $devCompose = Get-Content "docker-compose.yml" -Raw
        if ($devCompose -match "football-frontend-dev") {
            Write-Host "  ✅ Development service defined" -ForegroundColor Green
        } else {
            $warnings += "Development service name might be different"
        }
    } catch {
        $errors += "Failed to parse docker-compose.yml"
    }
}

# Production compose
if (Test-Path "docker-compose.prod.yml") {
    try {
        $prodCompose = Get-Content "docker-compose.prod.yml" -Raw
        if ($prodCompose -match "football-frontend-prod") {
            Write-Host "  ✅ Production service defined" -ForegroundColor Green
        } else {
            $warnings += "Production service name might be different"
        }
    } catch {
        $errors += "Failed to parse docker-compose.prod.yml"
    }
}

# Check package.json scripts
Write-Host ""
Write-Host "📦 Validating package.json scripts..." -ForegroundColor Yellow
if (Test-Path "package.json") {
    try {
        $packageJson = Get-Content "package.json" -Raw | ConvertFrom-Json
        $dockerScripts = @("docker:dev", "docker:prod", "docker:build:dev", "docker:build:prod")
        
        foreach ($script in $dockerScripts) {
            if ($packageJson.scripts.$script) {
                Write-Host "  ✅ Script: $script" -ForegroundColor Green
            } else {
                Write-Host "  ❌ Script: $script (missing)" -ForegroundColor Red
                $errors += "Missing npm script: $script"
            }
        }
    } catch {
        $errors += "Failed to parse package.json"
    }
}

# Check Next.js configuration
Write-Host ""
Write-Host "⚙️  Validating Next.js configuration..." -ForegroundColor Yellow
if (Test-Path "next.config.ts") {
    $nextConfig = Get-Content "next.config.ts" -Raw
    if ($nextConfig -match "output.*standalone") {
        Write-Host "  ✅ Standalone output configured" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  Standalone output not found" -ForegroundColor Yellow
        $warnings += "Consider adding 'output: standalone' for optimal Docker builds"
    }
}

# Summary
Write-Host ""
Write-Host "📊 Validation Summary" -ForegroundColor Cyan
Write-Host "===================" -ForegroundColor Cyan

if ($errors.Count -eq 0 -and $warnings.Count -eq 0) {
    Write-Host "🎉 Perfect! Docker configuration is complete and valid." -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Run: .\setup-docker.ps1 (if not done already)" -ForegroundColor White
    Write-Host "2. Start development: pnpm run docker:dev" -ForegroundColor White
    Write-Host "3. Or start production: pnpm run docker:prod" -ForegroundColor White
} elseif ($errors.Count -eq 0) {
    Write-Host "✅ Docker configuration is valid with minor warnings." -ForegroundColor Green
    Write-Host ""
    Write-Host "Warnings:" -ForegroundColor Yellow
    foreach ($warning in $warnings) {
        Write-Host "  ⚠️  $warning" -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ Docker configuration has errors that need to be fixed." -ForegroundColor Red
    Write-Host ""
    Write-Host "Errors:" -ForegroundColor Red
    foreach ($error in $errors) {
        Write-Host "  ❌ $error" -ForegroundColor Red
    }
    
    if ($warnings.Count -gt 0) {
        Write-Host ""
        Write-Host "Warnings:" -ForegroundColor Yellow
        foreach ($warning in $warnings) {
            Write-Host "  ⚠️  $warning" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
