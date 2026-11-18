<#
  local-check.ps1 — Prueba local con NVIDIA SuperVL (vía user-secrets) sin abrir el navegador.
  Ejecuta: parse JD -> inicia sesion -> responde con una respuesta fuerte -> imprime
  veredicto, puntajes y source. Deja evidencia de que NVIDIA SÍ conecta.

  Uso:
    powershell -ExecutionPolicy Bypass -File scripts/local-check.ps1
#>
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$apiUrl = 'http://127.0.0.1:5210'
$userId = "local-check-$([guid]::NewGuid().ToString('N'))"

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:LLM__Provider = 'OpenAICompatible'
$env:LLM__OpenAICompatible__EnableThinking = 'false'
$env:LLM__OpenAICompatible__MaxTokens = '4096'

$api = $null

function Wait-For-Http([string]$url, [string]$label) {
    $deadline = (Get-Date).AddSeconds(90)
    while ((Get-Date) -lt $deadline) {
        try { Invoke-WebRequest -UseBasicParsing -Uri $url -TimeoutSec 5 | Out-Null; Write-Host "$label listo."; return }
        catch { Start-Sleep -Milliseconds 1000 }
    }
    throw "$label no respondió."
}

try {
    $api = Start-Process -FilePath 'dotnet' -ArgumentList @(
        'run', '--project',
        (Join-Path $root 'backend/src/InterviewDrill.Api/InterviewDrill.Api.csproj'),
        '--urls', $apiUrl) -WorkingDirectory $root -PassThru -WindowStyle Hidden
    Wait-For-Http "$apiUrl/api/health" 'API'

    $jd = @"
Senior Backend Engineer
Responsibilities:
- Design production RAG systems using HNSW
- Build event-driven services with Kafka
Required skills: RAG, Kafka, observability, Polly, PII sanitization
"@
    Write-Host "`n[1/3] Parseando JD con NVIDIA SuperVL..." -ForegroundColor Cyan
    $profile = Invoke-RestMethod -Method Post -Uri "$apiUrl/api/jd/parse" `
        -ContentType 'application/json; charset=utf-8' -Body (@{ text = $jd } | ConvertTo-Json) -TimeoutSec 150
    Write-Host ("   role={0} | seniority={1} | skills={2} | source={3}" -f `
        $profile.role, $profile.seniority, (($profile.requiredSkills.name) -join ', '), $profile.profileSource)

    Write-Host "`n[2/3] Iniciando sesion de entrevista..." -ForegroundColor Cyan
    $start = Invoke-RestMethod -Method Post -Uri "$apiUrl/api/interview/sessions" `
        -ContentType 'application/json; charset=utf-8' `
        -Body (@{ userId = $userId; profile = $profile } | ConvertTo-Json -Depth 10) -TimeoutSec 150
    Write-Host ("   Pregunta: {0} (skill={1})" -f $start.question.text, $start.question.skillTarget)

    Write-Host "`n[3/3] Respondiendo (respuesta fuerte, cubre conceptos clave)..." -ForegroundColor Cyan
    $answer = @"
Para este skill uso una arquitectura production-ready:
- RAG con embeddings + HNSW para busqueda vectorial y semantic cache para bajar costo.
- Zero-Math: scores y pesos se calculan en C#, nunca los delego al LLM.
- Guardrails de relevancia (RelevanceScore < 0.78) + human-in-the-loop.
- PII sanitization antes de inferencia y de vectorizar/persistir (varbinary(max)).
- Resiliencia con Polly: retry con backoff + circuit breaker + manejo de HTTP 429.
- Observabilidad con OpenTelemetry/ActivitySource y latencia P95/P99.
- Anti-repeticion por hash normalizado + similitud coseno > 0.92 (in-memory).
"@
    $resp = Invoke-RestMethod -Method Post -Uri "$apiUrl/api/interview/sessions/$($start.sessionId)/answers" `
        -ContentType 'application/json; charset=utf-8' `
        -Body (@{ userId = $userId; answerText = $answer } | ConvertTo-Json -Depth 6) -TimeoutSec 180

    $f = $resp.feedback
    Write-Host ("   Veredicto: {0}" -f $f.verdict) -ForegroundColor Green
    Write-Host ("   OverallScore={0} | TechnicalDepth={1} | ProductionReadiness={2}" -f $f.overallScore, $f.technicalDepth, $f.productionReadiness)
    Write-Host ("   Cubiertos: {0}" -f (($f.coveredKeywords) -join ', '))
    Write-Host ("   Faltantes: {0}" -f (($f.missedConcepts) -join ', '))
    Write-Host ("   Consejo: {0}" -f $f.improvementAdvice)
    if ($resp.nextQuestion) { Write-Host ("   Siguiente pregunta: {0}" -f $resp.nextQuestion.text) }

    if ($profile.profileSource -ne 'llm' -and $f.verdict -eq $null) {
        throw 'No se obtuvo respuesta del modelo; verifica la llave (dotnet user-secrets list).'
    }
}
finally {
    if ($api) { Stop-Process -Id $api.Id -Force -ErrorAction SilentlyContinue; Write-Host "`nAPI detenida." -ForegroundColor Yellow }
}
