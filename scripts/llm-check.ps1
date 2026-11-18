<#
  llm-check.ps1 — Validación local de conectividad a un proveedor LLM OpenAI-compatible
  (NVIDIA NIM / OpenAI). NO hardcodea secretos: lee la API key de una variable de entorno.

  Uso:
    # NVIDIA NIM
    $env:NVIDIA_API_KEY = "nvapi-..."        # (o OPENAI_API_KEY si usas OpenAI)
    powershell -ExecutionPolicy Bypass -File scripts/llm-check.ps1 -Provider Nvidia

    # OpenAI
    $env:OPENAI_API_KEY = "sk-..."
    powershell -ExecutionPolicy Bypass -File scripts/llm-check.ps1 -Provider OpenAI

  Parámetros opcionales:
    -Model   "meta/llama-3.1-70b-instruct" (Nvidia) | "gpt-4o-mini" (OpenAI)
    -BaseUrl Override explícito de la base URL.
#>
[CmdletBinding()]
param(
    [ValidateSet('Nvidia', 'OpenAI', 'OpenCode', 'Custom')]
    [string]$Provider = 'Nvidia',
    [string]$Model,
    [string]$BaseUrl
)

$ErrorActionPreference = 'Stop'

switch ($Provider) {
    'OpenCode' {
        if (-not $BaseUrl) { $BaseUrl = 'https://opencode.ai/inference/openai/v1' }
        if (-not $Model)   { $Model   = 'glm-5.3-flash-free' }
        $key = $env:OPENCODE_API_KEY
        if (-not $key) { $key = $env:LLM__OpenAiCompatible__ApiKey }
    }
    'Nvidia' {
        if (-not $BaseUrl) { $BaseUrl = 'https://integrate.api.nvidia.com/v1' }
        if (-not $Model)   { $Model   = 'nvidia/nemotron-3.5-lightning-30b-a3b' }
        $key = $env:NVIDIA_API_KEY
        if (-not $key) { $key = $env:LLM__OpenAiCompatible__ApiKey }
    }
    'OpenAI' {
        if (-not $BaseUrl) { $BaseUrl = 'https://api.openai.com/v1' }
        if (-not $Model)   { $Model   = 'gpt-4o-mini' }
        $key = $env:OPENAI_API_KEY
        if (-not $key) { $key = $env:LLM__OpenAiCompatible__ApiKey }
    }
    'Custom' {
        if (-not $BaseUrl) { throw 'Custom requiere -BaseUrl.' }
        $key = $env:LLM__OpenAiCompatible__ApiKey
    }
}

if (-not $key) {
    Write-Host "FALTA la API key. Define NVIDIA_API_KEY / OPENAI_API_KEY o LLM__OpenAiCompatible__ApiKey." -ForegroundColor Red
    Write-Host "Además: la llave que pegaste en el chat debe ROTARSE por seguridad." -ForegroundColor Yellow
    exit 2
}

$url = $BaseUrl.TrimEnd('/') + '/chat/completions'
$headers = @{ Authorization = ('Bearer ' + $key) }
$bodyObj = [ordered]@{
    model       = $Model
    messages    = @(
        @{ role = 'system'; content = 'Responde en una sola frase.' },
        @{ role = 'user'; content = 'Ping de conectividad. Responde: OK' }
    )
    temperature = 0
    max_tokens  = 20
}
if ($Provider -eq 'Nvidia') {
    $bodyObj.max_tokens = 16384
    $bodyObj.temperature = 1
    $bodyObj.top_p = 0.95
    $bodyObj.chat_template_kwargs = @{ enable_thinking = $true }
}
$body = $bodyObj | ConvertTo-Json -Depth 5

Write-Host ('POST ' + $url + '  (model=' + $Model + ')') -ForegroundColor Cyan

try {
    $resp = Invoke-RestMethod -Method Post -Uri $url -Headers $headers -ContentType 'application/json' -Body $body -TimeoutSec 60
    $msg = $resp.choices[0].message
    $text = $msg.content
    if ([string]::IsNullOrWhiteSpace($text) -and $msg.reasoning_content) { $text = $msg.reasoning_content }
    Write-Host 'CONEXIÓN OK. Respuesta del modelo:' -ForegroundColor Green
    Write-Host ('  ' + $text)
    if ($resp.usage) {
        Write-Host ('  tokens: prompt=' + $resp.usage.prompt_tokens + ' completion=' + $resp.usage.completion_tokens) -ForegroundColor DarkGray
    }
    exit 0
}
catch {
    Write-Host 'FALLÓ la llamada:' -ForegroundColor Red
    Write-Host ('  ' + $_.Exception.Message)
    if ($_.ErrorDetails -and $_.ErrorDetails.Message) { Write-Host ('  ' + $_.ErrorDetails.Message) }
    exit 1
}
