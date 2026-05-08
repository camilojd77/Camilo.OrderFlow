<#
.SYNOPSIS
    Wrapper para levantar/derribar el contenedor SQL Server de desarrollo.

.DESCRIPTION
    Usa docker compose con deploy/database/docker-compose.sqlserver.yml.
    El contenedor expone SQL Server 2022 en localhost:1433.
    Credenciales por defecto: sa / Password123! (solo para desarrollo local).

.PARAMETER Command
    up   — Levanta el contenedor en background y espera a que esté healthy.
    down — Detiene y elimina el contenedor (los datos persisten en el volumen).
    logs — Muestra los logs del contenedor.

.EXAMPLE
    .\dev\database.ps1 up
    .\dev\database.ps1 down
    .\dev\database.ps1 logs
#>
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('up', 'down', 'logs')]
    [string] $Command
)

$ComposeFile = Join-Path $PSScriptRoot '..\deploy\database\docker-compose.sqlserver.yml'

switch ($Command) {
    'up' {
        Write-Host "Levantando SQL Server 2022..." -ForegroundColor Cyan
        docker compose -f $ComposeFile up -d
        Write-Host "Esperando a que SQL Server esté listo (puede tardar ~30 s)..." -ForegroundColor Yellow
        $retries = 0
        do {
            Start-Sleep -Seconds 5
            $retries++
            $status = docker inspect --format='{{.State.Health.Status}}' company-product-sqlserver 2>$null
        } while ($status -ne 'healthy' -and $retries -lt 12)

        if ($status -eq 'healthy') {
            Write-Host "SQL Server listo en localhost:1433 (sa / Password123!)" -ForegroundColor Green
        } else {
            Write-Warning "SQL Server tardó más de lo esperado. Revisa: .\dev\database.ps1 logs"
        }
    }
    'down' {
        Write-Host "Deteniendo SQL Server..." -ForegroundColor Cyan
        docker compose -f $ComposeFile down
        Write-Host "Contenedor detenido. Los datos persisten en el volumen Docker." -ForegroundColor Green
    }
    'logs' {
        docker compose -f $ComposeFile logs --follow
    }
}
