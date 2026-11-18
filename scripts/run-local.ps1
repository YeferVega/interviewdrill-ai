<#
  run-local.ps1 — Levanta API (.NET 11, modo Development) + frontend (Vite) para revisar local.
  Deja el proveedor LLM de NVIDIA SuperVL (user-secrets). No hardcodea secretos.

  Uso:
    powershell -ExecutionPolicy Bypass -File scripts/run-local.ps1
    # Heurístico determinista (sin red / sin gastar tokens):
    powershell -ExecutionPolicy Bypass -File scripts/run-local.ps1 -Heuristic

  URLs:
    Frontend: http://127.0.0.1:5174
    API:      http://127.0.0.1:5210  (health: /api/health)

  Ctrl+C termina ambos servidores.
#>
[CmdletBinding()]
param(
    [switch]$Heuristic,
    [string]$ApiUrl = 'http://127.0.0.1:5210',
    [string]$FrontendUrl = 'http://127.0.0.1:5174'
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

if ($Heuristic) {
    $env:LLM__Provider = 'Heuristic'
    Write-Host 'LLM: Heuristic (sin llamadas de red).'
} else {
    $env:LLM__Provider = 'OpenAICompatible'
    Write-Host 'LLM: NVIDIA OpenAI-compatible (SuperVL via user-secrets).'
}
$env:ASPNETCORE_ENVIRONMENT = 'Development'

$api = $null
$frontend = $null

function Wait-For-Http([string]$url, [string]$label) {
    $deadline = (Get-Date).AddSeconds(90)
    while ((Get-Date) -lt $deadline) {
        try {
            Invoke-WebRequest -UseBasicParsing -Uri $url -TimeoutSec 5 | Out-Null
            Write-Host "$label listo en $url"
            return
        } catch { Start-Sleep -Milliseconds 1000 }
    }
    throw "$label no respondió en $url."
}

try {
    $api = Start-Process -FilePath 'dotnet' -ArgumentList @(
        'run', '--project',
        (Join-Path $root 'backend/src/InterviewDrill.Api/InterviewDrill.Api.csproj'),
        '--no-build', '--urls', $ApiUrl) -WorkingDirectory $root -PassThru -WindowStyle Hidden
    Write-Host 'Iniciando API...'
    Wait-For-Http "$ApiUrl/api/health" 'API'

    # El proxy de Vite debe apuntar al puerto de esta API (vite.config.ts ya trae este
    # default; se fija aqui tambien por si el server del usuario es antiguo).
    $env:VITE_API_PROXY_TARGET = $ApiUrl
    $frontend = Start-Process -FilePath 'cmd.exe' -ArgumentList '/c', 'npm', 'run', 'dev', '--', '--host', '127.0.0.1', '--port', '5174' `
        -WorkingDirectory (Join-Path $root 'frontend') -PassThru -WindowStyle Hidden
    Write-Host 'Iniciando frontend...'
    Wait-For-Http $FrontendUrl 'Frontend'

    Write-Host ''
    Write-Host '=====================================================' -ForegroundColor Green
    Write-Host ('  Abre el navegador en: ' + $FrontendUrl) -ForegroundColor Green
    Write-Host ('  Health API:           ' + "$ApiUrl/api/health")
    Write-Host '  Presiona Ctrl+C para detener ambos servidores.'
    Write-Host '=====================================================' -ForegroundColor Green
    Write-Host ''

    # Bloquea hasta Ctrl+C; el finally hace cleanup.
    while ($true) { Start-Sleep -Seconds 5 }
}
finally {
    if ($api) { Stop-Process -Id $api.Id -Force -ErrorAction SilentlyContinue }
    if ($frontend) { Stop-Process -Id $frontend.Id -Force -ErrorAction SilentlyContinue }
    Write-Host 'Servidores detenidos.' -ForegroundColor Yellow
}
