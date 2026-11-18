# Constitution — InterviewDrill AI

Este documento rige la arquitectura, estándares de calidad, pruebas, experiencia de usuario, desempeño, seguridad, documentación y colaboración para el simulador de entrevistas técnicas GenAI.

---

## Core Principles

### 1. Code Quality

- **Pluggable providers:** Los proveedores LLM (Azure OpenAI / Azure Foundry, AWS Bedrock, Ollama) deben implementarse mediante la interfaz `ILlmCompletionClient` en `InterviewDrill.Core.Llm`. Cada proveedor vive en su propia clase bajo `InterviewDrill.Infrastructure.Llm`.
- **Zero-Math principle:** Todo cálculo de scoring (pesos 0.6/0.4, umbrales de veredicto, normalización 1-5 a 0-100) se hace en C# (`AnchoredRubricMath`, `HybridArchitectureEvaluator`). Nunca se delega puntuación numérica al LLM.
- **PII sanitization obligatoria:** `RegexPiiSanitizer` (`InterviewDrill.Core.Security`) se aplica antes de enviar datos al LLM y antes de persistir. Nunca se envían claves API, correos, teléfonos ni documentos de identidad sin sanitizar.
- **No hardcoded secrets:** Las configuraciones de proveedor se leen de `appsettings.json` y variables de entorno. Los valores por defecto son placeholders vacíos; los proveedores lanzan `InvalidOperationException` con instrucciones claras cuando faltan credenciales.
- **In-memory fallback:** Cuando no hay connection string de SQL Server ni Redis, todos los repositorios y caches usan implementaciones in-memory sin romper la aplicación.
- **Anti-repetición de preguntas:** El `QuestionRegistry` persiste preguntas ya formuladas por usuario+JD (`rawTextHash`) y deduplica exacta y semánticamente antes de emitir una nueva pregunta.
- **Separación de capas:** Core (contratos, entidades, opciones), Agents (lógica de agentes y jueces), Infrastructure (implementaciones concretas: EF, LLM clients, PDF, sanitizers), Api (endpoints, SignalR, DI).

### 2. Testing Standards

- **Cobertura mínima:** Todo nuevo código debe tener pruebas unitarias en el proyecto de pruebas correspondiente (`Core.Tests`, `Agents.Tests`, `Infrastructure.Tests`, `Api.Tests`).
- **Pruebas de proveedores LLM:** Usar `HttpMessageHandler` falso (`FakeHttpHandler`) con `BaseAddress` configurado cuando el cliente use URI relativa. Nunca llamar a servicios reales en pruebas unitarias.
- **Pruebas de jueces LLM:** El `FixedLlmClient` debe devolver JSON con el esquema exacto que espera `LlmArchitectureJudge` (`reasoning`, `clarity 1-5`, `technicalAccuracy 1-5`, `architectureDepth 1-5`, `communication 1-5`, `coveredKeywords`, `missedConcepts`, `strengths`, `detectedAntiPatterns`).
- **Constructor de `LlmCompletionRequest`:** Usar argumentos posicionales `(systemPrompt, userPrompt, responseJsonSchema?, temperature?, correlationId?)`. No usar named arguments que no existen.
- **Todas las pruebas deben pasar:** `dotnet test` debe reportar 0 fallos antes de considerar una tarea completa.
- **E2E smoke test:** `scripts/e2e-smoke.ps1` debe pasar: backend API, frontend, JD parse, entrevista adaptativa, guía de estudio, historial y exportación PDF.

### 3. User Experience Consistency

- **Frontend:** React 19 + Vite + TypeScript + Zustand. Páginas: chat (entrevista), estudio (guía), historial.
- **Backend API:** Endpoints REST bajo `/api` (jd/parse, interview/sessions, answers, study-guide, study-guide.pdf, history).
- **SignalR:** Comunicación en tiempo real para la entrevista (hub para mensajes push).
- **Respuestas estructuradas:** Toda salida de evaluación cumple el schema `EvaluationPayload` (overallScore, technicalDepth, productionReadiness, verdict, coveredKeywords, missedConcepts, strengths, improvementAdvice).
- **Veredictos claros:** PASS (>=80), NEEDS_PRACTICE (60-79), FAIL (<60).
- **Idioma:** Interfaz y mensajes en español.

### 4. Performance Requirements

- **Latencia P95/P99:** Monitorear llamadas al LLM; implementar caché semántica (Redis o in-memory fallback) para evitar re-inferencia de preguntas idénticas.
- **Rate limiting:** Manejo de HTTP 429 con retry/backoff (Polly) en proveedores LLM.
- **Carga de documentos:** Extracción de texto PDF/DOCX/TXT sin bloquear el hilo principal; usar `IJdDocumentTextExtractor`.
- **Embebidos:** `HashEmbeddingGenerator` como implementación por defecto sin dependencias externas pesadas.

### 5. Security

- **Sanitización previa:** `RegexPiiSanitizer` antes de inferencia o vectorización. Datos sensibles enmascarados antes de persistir.
- **Guardrails de relevancia:** `RelevanceScore < 0.78` activa intervención humana (human-in-the-loop).
- **Autenticación/autorización:** `userId` rastreado en todas las operaciones de sesión y respuesta.
- **Validación de entrada:** Todos los endpoints validan cuerpo de petición (no vacío, tipos correctos, archivos soportados para JD).
- **CORS y hosts permitidos:** `AllowedHosts` configurado en `appsettings.json`.

### 6. Documentation

- **Constitución viva:** Este archivo se actualiza cuando cambian los principios arquitectónicos.
- **AGENTS.md:** Documento de arquitectura multi-agente (topología, roles, contratos de datos, flujo de datos).
- **Comentarios en código:** Claves de configuración y notas de implementación pendientes (ej. Bedrock SigV4) documentadas en el código fuente.
- **Esquema JSON:** `EvaluationPayload` documentado y usado como contrato de salida de evaluación.

### 7. Collaboration

- **Flujo de desarrollo:** Specify → Plan → Tasks → Implement → Review → Deploy.
- **Revisión de código:** Todo cambio debe incluir pruebas y pasar `dotnet build` + `dotnet test` + E2E smoke.
- **Worktrees:** No se usan worktrees; se trabaja en un único directorio de trabajo.

---

## Development Workflow

1. **Specify:** Definir qué se va a construir (spec-kit specify).
2. **Plan:** Crear plan técnico (spec-kit plan).
3. **Tasks:** Desglosar en tareas ejecutables (spec-kit tasks).
4. **Implement:** Escribir código y pruebas (spec-kit implement).
5. **Review:** Ejecutar build, pruebas unitarias y E2E smoke.
6. **Deploy:** Desplegar backend y frontend.

---

## Technology Stack Guidelines

- **Backend:** .NET 11 (RC3+), ASP.NET Core, SignalR, EF Core, SQL Server/Azure SQL (opcional), in-memory fallback.
- **Frontend:** React 19, Vite, TypeScript, Zustand.
- **LLM providers:** Azure OpenAI / Azure Foundry, AWS Bedrock, Ollama (pluggable via `ILlmCompletionClient`).
- **Semantic cache:** Redis (opcional), in-memory fallback.
- **Document extraction:** PDF/DOCX/TXT via `IJdDocumentTextExtractor`.
- **PII sanitization:** `RegexPiiSanitizer` con patrones regex compilados.

---

## Definition of Done

Una tarea se considera completa cuando:

- [ ] El código compila sin errores (`dotnet build --nologo`).
- [ ] Todas las pruebas unitarias pasan (`dotnet test`, 0 fallos).
- [ ] El E2E smoke test pasa (`scripts/e2e-smoke.ps1`).
- [ ] No hay secretos hardcodeados; configuración vía `appsettings.json` / variables de entorno.
- [ ] El PII sanitizer se aplica en los puntos de entrada al LLM y persistencia.
- [ ] El scoring se calcula en C# (Zero-Math).
- [ ] Existe documentación o comentario para decisiones arquitectónicas no obvias.
- [ ] La constitución se actualiza si se introduce un nuevo principio o se modifica uno existente.
