<#
.SYNOPSIS
    Manages the local observability stack (OTel Collector + Tempo + Prometheus + Grafana).
.DESCRIPTION
    Thin wrapper around `docker compose` for the observability stack.
    All containers share the named network 'observability' and write to named volumes,
    so data persists between down/up cycles.
.PARAMETER Command
    up       – Start all containers in the background (default).
    down     – Stop and remove containers (volumes are preserved).
    restart  – down + up.
    logs     – Stream combined logs from all containers.
    status   – Show running state of each container.
    wipe     – Stop containers AND remove all named volumes (full reset).
.EXAMPLE
    ./dev/observability.ps1
    ./dev/observability.ps1 up
    ./dev/observability.ps1 down
    ./dev/observability.ps1 logs
    ./dev/observability.ps1 wipe
#>
param(
    [Parameter(Position = 0)]
    [ValidateSet("up", "down", "restart", "logs", "status", "wipe")]
    [string]$Command = "up"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Resolve paths ────────────────────────────────────────────────────────────
$SolutionRoot = Split-Path $PSScriptRoot -Parent
$ComposeFile  = Join-Path $SolutionRoot "deploy\observability\docker-compose.observability.yml"

if (-not (Test-Path $ComposeFile)) {
    Write-Error "Compose file not found: $ComposeFile"
    exit 1
}

# ── Helpers ──────────────────────────────────────────────────────────────────
function Write-Step  ([string]$msg) { Write-Host "  $msg" -ForegroundColor Cyan    }
function Write-Ok    ([string]$msg) { Write-Host "  $msg" -ForegroundColor Green   }
function Write-Warn  ([string]$msg) { Write-Host "  $msg" -ForegroundColor Yellow  }
function Write-Label ([string]$msg) { Write-Host "  $msg" -ForegroundColor DarkGray }

function Invoke-Compose {
    param([string[]]$Arguments)
    & docker compose -f $ComposeFile @Arguments
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

function Show-Urls {
    Write-Host ""
    Write-Ok    "Observability stack is running"
    Write-Host ""
    Write-Host  "  Service          URL" -ForegroundColor White
    Write-Label "  ────────────────────────────────────────────"
    Write-Host  "  Grafana          http://localhost:3000" -ForegroundColor White
    Write-Host  "  Prometheus       http://localhost:9090" -ForegroundColor White
    Write-Host  "  Tempo API        http://localhost:3200" -ForegroundColor White
    Write-Host  "  OTLP gRPC        http://localhost:4317" -ForegroundColor White
    Write-Host  "  OTLP HTTP        http://localhost:4318" -ForegroundColor White
    Write-Host  "  Collector stats  http://localhost:8888/metrics" -ForegroundColor White
    Write-Host ""
    Write-Label "  appsettings.json:"
    Write-Label '    "OtlpEndpoint":     "http://localhost:4317"'
    Write-Label '    "ExporterProtocol": "grpc"'
    Write-Host ""
    Write-Label "  Or run with the pre-configured launch profile:"
    Write-Label "    dotnet run --project Company.Product.WebApi --launch-profile `"Local+OTel`""
    Write-Host ""
}

# ── Commands ─────────────────────────────────────────────────────────────────
switch ($Command) {

    "up" {
        Write-Step "Starting local observability stack..."
        Invoke-Compose @("up", "-d", "--remove-orphans")
        Show-Urls
    }

    "down" {
        Write-Warn "Stopping local observability stack..."
        Invoke-Compose @("down")
        Write-Ok "Stack stopped. Named volumes retained."
    }

    "restart" {
        Write-Step "Restarting local observability stack..."
        Invoke-Compose @("down")
        Invoke-Compose @("up", "-d", "--remove-orphans")
        Show-Urls
    }

    "logs" {
        Write-Step "Streaming logs (Ctrl+C to exit)..."
        Invoke-Compose @("logs", "-f", "--tail=50")
    }

    "status" {
        Invoke-Compose @("ps")
    }

    "wipe" {
        Write-Warn "Wiping stack AND all named volumes (tempo-data, prometheus-data, grafana-data)..."
        $confirm = Read-Host "  Type 'yes' to confirm"
        if ($confirm -ne "yes") {
            Write-Host "  Aborted." -ForegroundColor DarkGray
            exit 0
        }
        Invoke-Compose @("down", "--volumes")
        Write-Ok "Stack and volumes removed."
    }
}
