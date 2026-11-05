<#
.SYNOPSIS
    Renombra la plantilla completa: Company.Product -> el nombre de tu proyecto.

.DESCRIPTION
    Sustituye los tres formatos del token en todos los archivos de texto del repositorio
    y renombra carpetas, .csproj y el .slnx.

        Company.Product   -> Acme.Inventory     (namespaces, proyectos, referencias)
        company-product   -> acme-inventory     (contenedores, compose, labels de Loki)
        CompanyProduct    -> AcmeInventory      (nombre de la base de datos)

    Lo que este script NO hace, a propósito (ver docs/04-Guia-Renombrado-y-Repositorio.md):
      · No toca .git ni configura el repositorio remoto.
      · No cambia el email del admin semilla ni los puertos.
      · No borra bin/ ni obj/ (hazlo antes, con el IDE cerrado).

    Ejecuta siempre primero con -WhatIf para ver el alcance.

.PARAMETER NewName
    Nombre nuevo en formato <Empresa>.<Producto>, en PascalCase. Ej.: Acme.Inventory

.PARAMETER Path
    Raíz del repositorio. Por defecto, la carpeta padre de este script.

.EXAMPLE
    .\dev\rename.ps1 -NewName "Acme.Inventory" -WhatIf
    .\dev\rename.ps1 -NewName "Acme.Inventory"
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string] $NewName,

    [string] $Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── Validación del nombre ────────────────────────────────────────────────────
if ($NewName -notmatch '^[A-Z][A-Za-z0-9]*(\.[A-Z][A-Za-z0-9]*)+$') {
    throw "NewName debe ser PascalCase con al menos dos segmentos separados por punto (ej. Acme.Inventory). Recibido: '$NewName'"
}

$OldDotted = 'Company.Product'
$OldKebab  = 'company-product'
$OldNoDots = 'CompanyProduct'

if ($NewName -eq $OldDotted) { throw "El nombre nuevo es igual al actual; no hay nada que hacer." }

$NewDotted = $NewName
$NewNoDots = $NewName -replace '\.', ''
# kebab-case: separa tambien en cada mayuscula interna (AcmeInventory -> acme-inventory)
$NewKebab  = (($NewName -replace '\.', '-') -creplace '(?<=[a-z0-9])(?=[A-Z])', '-').ToLowerInvariant()

if (-not $Path) { $Path = Split-Path $PSScriptRoot -Parent }
$Root = (Resolve-Path $Path).Path

# ── Qué archivos se procesan ─────────────────────────────────────────────────
$TextExtensions = @(
    '.cs', '.csproj', '.slnx', '.sln', '.json', '.props', '.targets', '.config',
    '.yml', '.yaml', '.ps1', '.sh', '.md', '.http', '.editorconfig', '.xml'
)
$ExtensionlessNames = @('Dockerfile', '.dockerignore', '.gitignore', '.gitattributes')
$ExcludedDirs = @('.git', '.vs', '.vscode', '.idea', '.claude', 'bin', 'obj', 'node_modules', 'TestResults', 'test-results', 'docs-develop', '.agents', '.codex', '.aws')
$SensitiveNames = @('appsettings.Development.json', 'appsettings.Local.json', 'appsettings.Production.json', 'appsettings.Staging.json', 'secrets.json')

# El propio script se excluye: contiene los tokens de busqueda y reescribirlos lo
# dejaria inservible para una segunda pasada.
$SelfPath = $MyInvocation.MyCommand.Path

function Test-Excluded {
    param([string] $FullPath)
    if ($SelfPath -and $FullPath -eq $SelfPath) { return $true }
    $relative = $FullPath.Substring($Root.Length).TrimStart('\', '/')
    # Keep local secrets and deployment data outside all rename passes.
    $leaf = Split-Path $FullPath -Leaf
    if ($SensitiveNames -contains $leaf) { return $true }
    if (($leaf -eq '.env' -or $leaf -like '.env.*') -and $leaf -notlike '*.example') { return $true }
    if ($relative -match '^deploy[\\/](?:.*[\\/])?(data|volumes|sqlserver-data|grafana-data|loki-data|tempo-data|prometheus-data)([\\/]|$)') { return $true }
    foreach ($segment in ($relative -split '[\\/]')) {
        if ($ExcludedDirs -contains $segment) { return $true }
    }
    return $false
}

Write-Host ""
Write-Host "  Renombrado de plantilla" -ForegroundColor Cyan
Write-Host "  ─────────────────────────────────────────────" -ForegroundColor DarkGray
Write-Host "  Raiz          : $Root"
Write-Host "  $OldDotted -> $NewDotted"
Write-Host "  $OldKebab -> $NewKebab"
Write-Host "  $OldNoDots  -> $NewNoDots"
if ($WhatIfPreference) { Write-Host "  MODO -WhatIf  : no se escribe nada" -ForegroundColor Yellow }
Write-Host ""

# ── 1. Contenido ─────────────────────────────────────────────────────────────
Write-Host "  [1/3] Sustituyendo contenido..." -ForegroundColor Cyan

$candidates = Get-ChildItem -Path $Root -Recurse -File |
    Where-Object {
        (-not (Test-Excluded $_.FullName)) -and
        (($TextExtensions -contains $_.Extension.ToLowerInvariant()) -or ($ExtensionlessNames -contains $_.Name))
    }

$changedFiles = 0
foreach ($file in $candidates) {
    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
    if ($bytes.Length -eq 0) { continue }

    $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
    $content = [System.Text.Encoding]::UTF8.GetString($bytes)
    if ($hasBom) { $content = $content.Substring(1) }

    $updated = $content.Replace($OldDotted, $NewDotted).Replace($OldKebab, $NewKebab).Replace($OldNoDots, $NewNoDots)
    if ($updated -eq $content) { continue }

    if ($PSCmdlet.ShouldProcess($file.FullName, "Sustituir tokens")) {
        $encoding = New-Object System.Text.UTF8Encoding($hasBom)
        [System.IO.File]::WriteAllText($file.FullName, $updated, $encoding)
    }
    $changedFiles++
    Write-Verbose "    $($file.FullName.Substring($Root.Length + 1))"
}
Write-Host "        $changedFiles archivos con cambios de contenido" -ForegroundColor Green

# ── 2. Archivos ──────────────────────────────────────────────────────────────
Write-Host "  [2/3] Renombrando archivos..." -ForegroundColor Cyan

$renamedFiles = 0
$filesToRename = Get-ChildItem -Path $Root -Recurse -File |
    Where-Object { (-not (Test-Excluded $_.FullName)) -and ($_.Name -like "*$OldDotted*") }

foreach ($file in $filesToRename) {
    $newFileName = $file.Name.Replace($OldDotted, $NewDotted)
    if ($PSCmdlet.ShouldProcess($file.FullName, "Renombrar a $newFileName")) {
        Rename-Item -LiteralPath $file.FullName -NewName $newFileName
    }
    $renamedFiles++
    Write-Verbose "    $($file.Name) -> $newFileName"
}
Write-Host "        $renamedFiles archivos renombrados" -ForegroundColor Green

# ── 3. Carpetas (de la más profunda a la más superficial) ────────────────────
Write-Host "  [3/3] Renombrando carpetas..." -ForegroundColor Cyan

$renamedDirs = 0
$dirsToRename = Get-ChildItem -Path $Root -Recurse -Directory |
    Where-Object { (-not (Test-Excluded $_.FullName)) -and ($_.Name -like "*$OldDotted*") } |
    Sort-Object { $_.FullName.Length } -Descending

foreach ($dir in $dirsToRename) {
    $newDirName = $dir.Name.Replace($OldDotted, $NewDotted)
    if ($PSCmdlet.ShouldProcess($dir.FullName, "Renombrar a $newDirName")) {
        Rename-Item -LiteralPath $dir.FullName -NewName $newDirName
    }
    $renamedDirs++
    Write-Verbose "    $($dir.Name) -> $newDirName"
}
Write-Host "        $renamedDirs carpetas renombradas" -ForegroundColor Green

# ── Cierre ───────────────────────────────────────────────────────────────────
Write-Host ""
if ($WhatIfPreference) {
    Write-Host "  Simulacion terminada. Vuelve a ejecutar sin -WhatIf para aplicar." -ForegroundColor Yellow
    return
}

Write-Host "  Renombrado aplicado. Pendiente a mano:" -ForegroundColor Green
Write-Host "    Review local product settings selectively; preserve credentials." -ForegroundColor White
Write-Host "    Preserve root, Git history, remotes, demo email and ports unless explicitly authorized." -ForegroundColor White
Write-Host ""
Write-Host "  Verificacion:" -ForegroundColor Cyan
Write-Host "    Inspect active references, including hidden and untracked files; inventory historical/source exceptions." -ForegroundColor DarkGray
Write-Host "    dotnet restore; dotnet build; dotnet test -- --filter-not-trait Category=RealDb --ignore-exit-code 8" -ForegroundColor DarkGray
Write-Host ""
