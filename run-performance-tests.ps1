# Performance Test Runner Script
# This script provides easy commands to run different types of performance tests

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("load", "stress", "cache", "search", "benchmark", "all", "help")]
    [string]$TestType = "help",

    [Parameter(Mandatory = $false)]
    [string]$OutputPath = ".\performance-results",

    [Parameter(Mandatory = $false)]
    [string]$LogFilePath = $null,

    [Parameter(Mandatory = $false)]
    [switch]$OpenResults
)

# Check for --help or -h in $args
if ($args -contains "--help" -or $args -contains "-h") {
    Show-Help
    exit 0
}

# Prompt for TestType if not provided or is "help"
if ([string]::IsNullOrWhiteSpace($TestType) -or $TestType -eq "help") {
    $TestType = Read-Host "Enter test type (load, stress, cache, search, benchmark, all, help)"
}

# Prompt for OutputPath if not provided
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Read-Host "Enter output path (default: .\performance-results)"
    if ([string]::IsNullOrWhiteSpace($OutputPath)) {
        $OutputPath = ".\performance-results"
    }
}

# Prompt for LogFilePath if not provided
if ([string]::IsNullOrWhiteSpace($LogFilePath)) {
    $LogFilePath = Read-Host "Enter log file path (optional, press Enter to skip)"
    if ([string]::IsNullOrWhiteSpace($LogFilePath)) {
        $LogFilePath = $null
    }
}
if ($LogFilePath -eq "") {
    $LogFilePath = $null
}

function Write-Header
{
    param([string]$Title)
    Write-Host ""
    Write-Host "=" * 60 -ForegroundColor Cyan
    Write-Host " $Title" -ForegroundColor Yellow
    Write-Host "=" * 60 -ForegroundColor Cyan
    Write-Host ""
}

function Write-Info
{
    param([string]$Message)
    Write-Host $Message -ForegroundColor Green
}

function Write-Warning
{
    param([string]$Message)
    Write-Host $Message -ForegroundColor Yellow
}

function Write-Error
{
    param([string]$Message)
    Write-Host $Message -ForegroundColor Red
}

function Ensure-Directory
{
    param([string]$Path)
    if (!(Test-Path $Path))
    {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
        Write-Info "Created directory: $Path"
    }
}

# Function to run a command and optionally log its output
function Invoke-LoggedCommand {
    param(
        [string]$Command,
        [string[]]$Arguments,
        [string]$LogFile = $null
    )

    if ($null -ne $LogFile) {
        Write-Info "Executing: $Command $($Arguments -join ' ') | Out-File -Append -FilePath $LogFile"
        Invoke-Expression "$Command $($Arguments -join ' ') | Out-File -Append -FilePath '$LogFile'"
    } else {
        Write-Info "Executing: $Command $($Arguments -join ' ')"
        & $Command @Arguments
    }
}

function Run-LoadTests
{
    Write-Header "Running Load Tests (NBomber)"

    Write-Info "Building project..."
    Invoke-LoggedCommand "dotnet" @("build", "Footex.PerformanceTests", "--configuration", "Release") $LogFilePath

    if ($LASTEXITCODE -ne 0)
    {
        Write-Error "Build failed. Please fix compilation errors."
        return
    }

    Write-Info "Running API Load Tests..."
    Invoke-LoggedCommand "dotnet" @("test", "Footex.PerformanceTests", "--filter", "ClassName=ApiLoadTests", "--logger", "console;verbosity=detailed") $LogFilePath

    Write-Info "Running Cache Performance Tests..."
    Invoke-LoggedCommand "dotnet" @("test", "Footex.PerformanceTests", "--filter", "ClassName=CachePerformanceTests", "--logger", "console;verbosity=detailed") $LogFilePath

    Write-Info "Running Search Performance Tests..."
    Invoke-LoggedCommand "dotnet" @("test", "Footex.PerformanceTests", "--filter", "ClassName=SearchPerformanceTests", "--logger", "console;verbosity=detailed") $LogFilePath
}

function Run-StressTests
{
    Write-Header "Running Stress Tests (NBomber)"
    Write-Warning "Stress tests may take 30+ minutes to complete"

    $continue = Read-Host "Continue? (y/N)"
    if ($continue -ne "y" -and $continue -ne "Y")
    {
        Write-Info "Stress tests cancelled."
        return
    }

    Write-Info "Building project..."
    Invoke-LoggedCommand "dotnet" @("build", "Footex.PerformanceTests", "--configuration", "Release") $LogFilePath

    Write-Info "Running Stress Tests..."
    Invoke-LoggedCommand "dotnet" @("test", "Footex.PerformanceTests", "--filter", "ClassName=StressTests", "--logger", "console;verbosity=detailed") $LogFilePath
}

function Run-Benchmarks
{
    Write-Header "Running Benchmarks (BenchmarkDotNet)"

    Write-Info "Building project in Release mode..."
    Invoke-LoggedCommand "dotnet" @("build", "Footex.PerformanceTests", "--configuration", "Release") $LogFilePath

    if ($LASTEXITCODE -ne 0)
    {
        Write-Error "Build failed. Please fix compilation errors."
        return
    }

    Write-Info "Running API Benchmarks..."
    Invoke-LoggedCommand "dotnet" @("run", "--project", "Footex.PerformanceTests", "--configuration", "Release", "api") $LogFilePath

    Write-Info "Running Search Benchmarks..."
    Invoke-LoggedCommand "dotnet" @("run", "--project", "Footex.PerformanceTests", "--configuration", "Release", "search") $LogFilePath

    Write-Info "Running Cache Benchmarks..."
    Invoke-LoggedCommand "dotnet" @("run", "--project", "Footex.PerformanceTests", "--configuration", "Release", "cache") $LogFilePath
}

function Show-Help
{
    Write-Header "Footex API Performance Test Runner"

    Write-Host "USAGE:" -ForegroundColor Yellow
    Write-Host "  .\run-performance-tests.ps1 -TestType <type> [-OutputPath <path>] [-LogFilePath <path>] [-OpenResults]"
    Write-Host ""

    Write-Host "TEST TYPES:" -ForegroundColor Yellow
    Write-Host "  load      - Run load tests (API endpoints under normal load)"
    Write-Host "  stress    - Run stress tests (extreme load, spikes, endurance)"
    Write-Host "  cache     - Run cache-specific performance tests"
    Write-Host "  search    - Run search functionality performance tests"
    Write-Host "  benchmark - Run detailed micro-benchmarks"
    Write-Host "  all       - Run all performance tests (takes 1+ hour)"
    Write-Host "  help      - Show this help message"
    Write-Host ""

    Write-Host "OPTIONS:" -ForegroundColor Yellow
    Write-Host "  -OutputPath   Specify output directory for results (default: .\performance-results)"
    Write-Host "  -LogFilePath  Specify a file path to record all CLI output (e.g., C:\logs\perf_run.log)"
    Write-Host "  -OpenResults  Open results folder after completion"
    Write-Host ""

    Write-Host "EXAMPLES:" -ForegroundColor Yellow
    Write-Host "  .\run-performance-tests.ps1 -TestType load"
    Write-Host "  .\run-performance-tests.ps1 -TestType benchmark -OpenResults -LogFilePath .\benchmark_log.txt"
    Write-Host "  .\run-performance-tests.ps1 -TestType all -OutputPath C:\temp\perf-results -LogFilePath C:\logs\full_perf_run.log"
    Write-Host ""

    Write-Host "PREREQUISITES:" -ForegroundColor Yellow
    Write-Host "  - Docker Desktop running (for test database)"
    Write-Host "  - .NET 8 SDK installed"
    Write-Host "  - Solution built successfully"
    Write-Host ""

    Write-Host "PERFORMANCE TARGETS:" -ForegroundColor Yellow
    Write-Host "  - Health Check: < 50ms"
    Write-Host "  - Simple GET: < 200ms"
    Write-Host "  - Complex Queries: < 500ms"
    Write-Host "  - Search Operations: < 300ms"
    Write-Host "  - Cache Hit Ratio: > 80%"
}

function Check-Prerequisites
{
    Write-Info "Checking prerequisites..."

    # Check Docker
    docker version | Out-Null
    if ($LASTEXITCODE -eq 0)
    {
        Write-Info "✓ Docker is running"
    }
    else
    {
        Write-Warning "Docker may not be running. Some tests may fail."
    }

    # Check .NET
    dotnet --version > $null
    if ($LASTEXITCODE -eq 0)
    {
        $dotnetVersion = dotnet --version
        Write-Info "✓ .NET SDK: $dotnetVersion"
    }
    else
    {
        Write-Error "✗ .NET SDK not found. Please install .NET 8 SDK."
        return $false
    }

    Write-Host "Script running in directory: $(Get-Location)" -ForegroundColor Magenta

    # Check if in correct directory
    if (!(Test-Path "Footex.sln"))
    {
        Write-Error "✗ Please run this script from the solution root directory."
        return $false
    }

    Write-Info "✓ All prerequisites met"
    return $true
}

function Open-ResultsFolder
{
    if (Test-Path $OutputPath)
    {
        Write-Info "Opening results folder..."
        Start-Process $OutputPath
    }
    else
    {
        Write-Warning "Results folder not found: $OutputPath"
    }
}

# Main execution
Write-Header "Footex API Performance Test Runner"

if (-not (Check-Prerequisites))
{
    exit 1
}

Ensure-Directory $OutputPath

# If a log file path is provided, ensure the directory exists and clear existing content
if (![string]::IsNullOrWhiteSpace($LogFilePath)) {
    $logDirectory = Split-Path -Parent $LogFilePath
    Ensure-Directory $logDirectory
    if (Test-Path $LogFilePath) {
        Clear-Content $LogFilePath
        Write-Info "Cleared existing log file: $LogFilePath"
    }
    Write-Info "All CLI output will be recorded to: $LogFilePath"
}


switch ( $TestType.ToLower())
{
    "load" {
        Run-LoadTests
    }
    "stress" {
        Run-StressTests
    }
    "cache" {
        Write-Info "Running cache-specific tests..."
        Invoke-LoggedCommand "dotnet" @("test", "Footex.PerformanceTests", "--filter", "ClassName=CachePerformanceTests", "--logger", "console;verbosity=detailed") $LogFilePath
        Invoke-LoggedCommand "dotnet" @("run", "--project", "Footex.PerformanceTests", "--configuration", "Release", "cache") $LogFilePath
    }
    "search" {
        Write-Info "Running search-specific tests..."
        Invoke-LoggedCommand "dotnet" @("test", "Footex.PerformanceTests", "--filter", "ClassName=SearchPerformanceTests", "--logger", "console;verbosity=detailed") $LogFilePath
        Invoke-LoggedCommand "dotnet" @("run", "--project", "Footex.PerformanceTests", "--configuration", "Release", "search") $LogFilePath
    }
    "benchmark" {
        Run-Benchmarks
    }
    "all" {
        Write-Warning "Running ALL performance tests. This will take 1+ hour."
        $continue = Read-Host "Continue? (y/N)"
        if ($continue -eq "y" -or $continue -eq "Y")
        {
            Run-LoadTests
            Run-StressTests
            Run-Benchmarks
        }
        else
        {
            Write-Info "All tests cancelled."
        }
    }
    "help" {
        Show-Help
    }
    default {
        Write-Error "Unknown test type: $TestType"
        Show-Help
        exit 1
    }
}

if ($OpenResults)
{
    Open-ResultsFolder
}

Write-Header "Performance Testing Complete"
Write-Info "Results can be found in: $OutputPath"
Write-Info "For detailed analysis, check the generated HTML reports."