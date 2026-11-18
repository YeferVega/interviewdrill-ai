[CmdletBinding()]
param(
    [switch]$UseDatabase,
    [switch]$WithLlm
)

$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$apiUrl = 'http://127.0.0.1:5210'
$frontendUrl = 'http://127.0.0.1:5174'
$userId = "e2e-$([guid]::NewGuid().ToString('N'))"

# Determinismo por defecto: sin -WithLlm la app corre en modo Heuristic (fallback determinista,
# sin llamadas de red). Se limpian las variables que podrían activar un proveedor externo.
# Con -WithLlm se deja el proveedor configurado por entorno (ej. LLM__Provider=OpenAICompatible
# + NVIDIA_API_KEY) para validar el flujo con un modelo real.
if (-not $WithLlm) {
    $env:LLM__Provider = 'Heuristic'
    Remove-Item Env:\NVIDIA_API_KEY -ErrorAction SilentlyContinue
    Remove-Item Env:\OPENAI_API_KEY -ErrorAction SilentlyContinue
    Remove-Item Env:\OPENCODE_API_KEY -ErrorAction SilentlyContinue
    Remove-Item Env:\LLM__OpenAICompatible__ApiKey -ErrorAction SilentlyContinue
    Write-Host 'E2E LLM mode: Heuristic (fallback determinista; use -WithLlm para modelo real).'
} else {
    # Con modelo real usamos SuperVL como base pero desactivamos "thinking" y limitamos
    # tokens para que el E2E corra en tiempo acotado (los jueces no necesitan razonamiento extenso).
    $env:LLM__OpenAICompatible__EnableThinking = 'false'
    $env:LLM__OpenAICompatible__MaxTokens = '4096'
    Write-Host 'E2E LLM mode: proveedor real desde entorno (API key + config, thinking OFF, max_tokens=4096).'
}

if ($UseDatabase) {
    # Opción B: la variable de entorno tiene prioridad (p. ej. Azure/AWS o una instancia
    # distinta); si no está definida, hace fallback a la instancia local SQLEXPRESS de pruebas.
    $localFallback = 'Server=localhost\SQLEXPRESS;Database=InterviewDrill_Testing;Trusted_Connection=True;TrustServerCertificate=True;'

    if ([string]::IsNullOrWhiteSpace($env:INTERVIEWDRILL_CONNECTION_STRING)) {
        $env:INTERVIEWDRILL_CONNECTION_STRING = $localFallback
        Write-Host 'E2E mode: SQL Server (fallback localhost\SQLEXPRESS, InterviewDrill_Testing)'
    } else {
        Write-Host 'E2E mode: SQL Server (usando INTERVIEWDRILL_CONNECTION_STRING del entorno)'
    }
} else {
    # Fuerza modo in-memory eliminando la variable del proceso (y por tanto del hijo),
    # incluso si quedó definida en la sesión del shell.
    Remove-Item Env:\INTERVIEWDRILL_CONNECTION_STRING -ErrorAction SilentlyContinue
    $env:INTERVIEWDRILL_CONNECTION_STRING = $null
    Write-Host 'E2E mode: in-memory'
}

$api = $null
$frontend = $null

function Wait-For-Http([string]$url, [string]$label) {
    $deadline = (Get-Date).AddSeconds(90)

    while ((Get-Date) -lt $deadline) {
        try {
            Invoke-WebRequest -UseBasicParsing -Uri $url -TimeoutSec 5 | Out-Null
            Write-Host "$label is ready at $url"
            return
        } catch {
            Start-Sleep -Milliseconds 1000
        }
    }

    throw "$label did not become ready at $url."
}

# Runs one complete adaptive interview and returns the session id, the list of
# question texts asked (in order), and the study guide markdown.
function Invoke-InterviewRun([string]$ApiUrl, [string]$UserId, $Profile) {
    $questions = New-Object System.Collections.Generic.List[string]

    $start = Invoke-RestMethod `
        -Method Post `
        -Uri "$ApiUrl/api/interview/sessions" `
        -ContentType 'application/json; charset=utf-8' `
        -Body (@{ userId = $UserId; profile = $Profile } | ConvertTo-Json -Depth 10)

    if (-not $start.sessionId -or -not $start.question) {
        throw 'Interview start did not return sessionId and question.'
    }

    $questions.Add([string]$start.question.text)

    $completed = $false
    $lastAnswer = $null
    $maxTurns = 25

    for ($i = 0; $i -lt $maxTurns; $i++) {
        $skill = if ($lastAnswer) { $lastAnswer.nextQuestion.skillTarget } else { $start.question.skillTarget }

        $answerText = @"
Para $skill uso una arquitectura production-ready: embeddings con HNSW, guardrails de relevancia, PII sanitization, Polly retry con backoff, rate limiting para HTTP 429, observabilidad con OpenTelemetry, latencia P95/P99, semantic cache y rollback plan.
"@

        $lastAnswer = Invoke-RestMethod `
            -Method Post `
            -Uri "$ApiUrl/api/interview/sessions/$($start.sessionId)/answers" `
            -ContentType 'application/json; charset=utf-8' `
            -Body (@{ userId = $UserId; answerText = $answerText } | ConvertTo-Json -Depth 6)

        if ($lastAnswer.interviewCompleted) {
            $completed = $true
            break
        }

        if (-not $lastAnswer.nextQuestion) {
            throw "Answer turn $($i + 1) returned no nextQuestion before interviewCompleted."
        }

        $questions.Add([string]$lastAnswer.nextQuestion.text)
    }

    if (-not $completed) {
        throw "Interview did not complete after $maxTurns answers."
    }

    if (-not $lastAnswer.studyGuideMarkdown) {
        throw 'Completed interview did not return studyGuideMarkdown.'
    }

    return @{ sessionId = $start.sessionId; questions = $questions; guide = $lastAnswer.studyGuideMarkdown }
}

try {
    $api = Start-Process `
        -FilePath 'dotnet' `
        -ArgumentList @(
            'run',
            '--project',
            (Join-Path $root 'backend/src/InterviewDrill.Api/InterviewDrill.Api.csproj'),
            '--no-build',
            '--urls',
            $apiUrl
        ) `
        -WorkingDirectory $root `
        -PassThru `
        -WindowStyle Hidden

    Wait-For-Http "$apiUrl/api/health" 'Backend API'

    $health = Invoke-RestMethod -Method Get -Uri "$apiUrl/api/health"
    if ($UseDatabase -and $health.persistence -ne 'sql-server') {
        throw "E2E DB mode expected persistence='sql-server' but got '$($health.persistence)'. Check INTERVIEWDRILL_CONNECTION_STRING and that the migration is applied."
    }

    if (-not $UseDatabase -and $health.persistence -ne 'in-memory') {
        throw "E2E in-memory mode expected persistence='in-memory' but got '$($health.persistence)'."
    }

    # El navegador habla con la API a traves del proxy de Vite (/api -> API_URL). Si el
    # proxy apunta a otro puerto, el usuario ve 502; se valida aqui para no regredir.
    $env:VITE_API_PROXY_TARGET = $apiUrl
    $frontend = Start-Process `
        -FilePath 'cmd.exe' `
        -ArgumentList '/c', 'npm', 'run', 'dev', '--', '--host', '127.0.0.1', '--port', '5174' `
        -WorkingDirectory (Join-Path $root 'frontend') `
        -PassThru `
        -WindowStyle Hidden

    Wait-For-Http $frontendUrl 'Frontend dev server'

    $proxyHealth = Invoke-WebRequest -UseBasicParsing -Uri "$frontendUrl/api/health" -TimeoutSec 20
    if ($proxyHealth.StatusCode -ne 200) {
        throw "Vite proxy check failed: GET $frontendUrl/api/health devolvio $($proxyHealth.StatusCode)."
    }
    Write-Host "Vite proxy OK: $frontendUrl/api -> $apiUrl"

    $frontendHtml = (Invoke-WebRequest -UseBasicParsing -Uri $frontendUrl -TimeoutSec 15).Content
    if ($frontendHtml -notmatch 'InterviewDrill') {
        throw 'Frontend HTML did not contain InterviewDrill brand.'
    }

    $jdText = @"
Senior Backend Engineer
Responsibilities:
- Design production RAG systems using HNSW
- Build event-driven services with Kafka
Required skills: RAG, Kafka, observability, Polly, PII sanitization
Nice to have Kubernetes
"@

    $profile = Invoke-RestMethod `
        -Method Post `
        -Uri "$apiUrl/api/jd/parse" `
        -ContentType 'application/json; charset=utf-8' `
        -Body (@{ text = $jdText } | ConvertTo-Json -Depth 6)

    if (-not $profile.requiredSkills) {
        throw 'JD parse did not return required skills.'
    }

    # --- Primera sesión completa ---
    $run1 = Invoke-InterviewRun $apiUrl $userId $profile
    $firstSessionId = $run1.sessionId

    $history = Invoke-RestMethod -Method Get -Uri "$apiUrl/api/interview/sessions?userId=$([uri]::EscapeDataString($userId))"
    if ($history.Count -lt 1) {
        throw 'History did not return the completed session.'
    }

    $session = $history | Where-Object { $_.sessionId -eq $firstSessionId } | Select-Object -First 1
    if (-not $session) {
        throw 'History did not include the completed session.'
    }

    if ($session.status -ne 'Completed') {
        throw "History session status is '$($session.status)', expected 'Completed'."
    }

    $guide = Invoke-RestMethod -Method Get -Uri "$apiUrl/api/interview/sessions/$firstSessionId/study-guide"
    if (-not $guide.markdown) {
        throw 'Study guide endpoint did not return markdown.'
    }

    $pdfResponse = Invoke-WebRequest -UseBasicParsing -Uri "$apiUrl/api/interview/sessions/$firstSessionId/study-guide.pdf" -TimeoutSec 15
    $pdfBytes = if ($pdfResponse.RawContentStream) {
        $pdfResponse.RawContentStream.ToArray()
    } else {
        [byte[]]$pdfResponse.Content
    }

    if ($pdfBytes.Length -lt 5) {
        throw 'Study guide PDF response was empty.'
    }

    $pdfHeader = [Text.Encoding]::ASCII.GetString($pdfBytes[0..4])
    if ($pdfHeader -ne '%PDF-') {
        throw "Study guide PDF did not have the expected header. Got '$pdfHeader'."
    }

    # --- Segunda sesión con el mismo usuario + mismo JD ---
    # Verifica la anti-repetición: no debe repetir ninguna pregunta de la primera sesión.
    $run2 = Invoke-InterviewRun $apiUrl $userId $profile

    if ($run2.sessionId -eq $firstSessionId) {
        throw 'Second interview returned the same sessionId as the first run.'
    }

    $overlap = @($run2.questions | Where-Object { $run1.questions -contains $_ })
    if ($overlap.Count -gt 0) {
        $duplicated = ($overlap | Select-Object -First 3) -join ' | '
        throw "Anti-repetition failed: the second session repeated questions from the first session: $duplicated"
    }

    if ($run2.questions.Count -eq 0) {
        throw 'Second interview did not ask any questions.'
    }

    # --- Verificacion bilingue (spec 021, solo en modo heuristico determinista) ---
    if (-not $WithLlm) {
        $inverted = [string][char]0x00BF

        $esStart = Invoke-RestMethod -Method Post -Uri "$apiUrl/api/interview/sessions" -ContentType 'application/json; charset=utf-8' -Body (@{ userId = "$userId-es"; profile = $profile; language = 'es' } | ConvertTo-Json -Depth 10)
        $enStart = Invoke-RestMethod -Method Post -Uri "$apiUrl/api/interview/sessions" -ContentType 'application/json; charset=utf-8' -Body (@{ userId = "$userId-en"; profile = $profile; language = 'en' } | ConvertTo-Json -Depth 10)

        if ($esStart.question.text -notlike "*$inverted*") {
            throw "E2E bilingual failed: language=es question is not Spanish: $($esStart.question.text)"
        }

        if ($enStart.question.text -like "*$inverted*" -or $enStart.question.text -notmatch '(?i)(production|diagnose|design|explain|how)') {
            throw "E2E bilingual failed: language=en question is not English: $($enStart.question.text)"
        }

        Write-Host 'E2E bilingual OK: language=es asks in Spanish, language=en asks in English.'
    }

    $mode = if ($UseDatabase) { 'SQL Server (InterviewDrill_Testing)' } else { 'in-memory' }
    Write-Host ("E2E anti-repetition OK [{0}]: session 2 asked {1} distinct questions, 0 overlap with session 1." -f $mode, $run2.questions.Count)
    Write-Host 'E2E smoke passed: backend API, frontend server, JD parse, adaptive interview, guide, history, PDF and anti-repetition.'
}
finally {
    if ($api) {
        Stop-Process -Id $api.Id -Force -ErrorAction SilentlyContinue
    }

    if ($frontend) {
        Stop-Process -Id $frontend.Id -Force -ErrorAction SilentlyContinue
    }
}
