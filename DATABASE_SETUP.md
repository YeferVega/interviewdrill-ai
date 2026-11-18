# Database Setup — InterviewDrill AI

## Your environment

| Item | Value |
|----------|-------|
| Server | `localhost\SQLEXPRESS` |
| Authentication | Windows (Trusted_Connection) |
| Admin user | `YOUR-MACHINE\your-user` |
| Local database | `InterviewDrill_Testing` |
| Production database | `InterviewDrill_Production` (Azure SQL / AWS RDS) |

## Configured connection strings

| Environment | Connection string |
|---------|-------------------|
| **Local / Testing** | `Server=localhost\SQLEXPRESS;Database=InterviewDrill_Testing;Trusted_Connection=True;TrustServerCertificate=True;` |
| **Production (Azure/AWS)** | `Server=tcp:YOUR_SERVER.database.windows.net,1433;Initial Catalog=InterviewDrill_Production;Persist Security Info=False;User ID=YOUR_USER;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;` |

---

## Option A: Create the database using SSMS (recommended to get started)

Run this script in **SQL Server Management Studio** connected to `localhost\SQLEXPRESS` with your user `YOUR-MACHINE\your-user`:

```sql
-- Crear la base de datos de testing
CREATE DATABASE InterviewDrill_Testing;
GO

-- Crear la base de datos de producción (opcional, para Azure/AWS después)
-- CREATE DATABASE InterviewDrill_Production;
-- GO
```

Then apply the Entity Framework migrations:

```powershell
# Desde la carpeta raíz del proyecto (donde está InterviewDrill.slnx)
dotnet ef database update --project backend/src/InterviewDrill.Infrastructure --startup-project backend/src/InterviewDrill.Api
```

---

## Option B: Create everything with EF Core (no SSMS)

If you already have SQL Server Express running, apply the migrations directly:

```powershell
# 1. Instalar la herramienta EF Core (una sola vez)
dotnet tool install --global dotnet-ef

# 2. Aplicar migraciones a la base de datos local
dotnet ef database update --project backend/src/InterviewDrill.Infrastructure --startup-project backend/src/InterviewDrill.Api
```

This will automatically create:
- The `InterviewDrill_Testing` database
- All tables: Users, JobDescriptions, InterviewSessions, Questions, Answers, StudyGuides
- Vector indexes for semantic search

---

## Project files

| File | Contents |
|---------|-----------|
| `backend/src/InterviewDrill.Api/appsettings.json` | `DefaultConnection` pointing to your local SQL Server; `ProductionConnection` with placeholders for Azure/AWS |
| `backend/src/InterviewDrill.Api/appsettings.Development.json` | `DefaultConnection` for the development environment |
| `backend/src/InterviewDrill.Api/Program.cs` | Uses SQL Server if `INTERVIEWDRILL_CONNECTION_STRING` is set; otherwise uses in-memory |

---

## Existing EF Core migrations

| Migration | Description |
|-----------|-------------|
| `20261113225952_InitialCreate` | Users, JobDescriptions, InterviewSessions, Questions (with `vector(1536)` + cosine index), Answers, StudyGuides |
| `20261115175243_PersistInterviewerStateAndAnswers` | Interview state and candidate answers |

---

## Useful commands

```powershell
# Ver estado de migraciones pendientes
dotnet ef migrations list --project backend/src/InterviewDrill.Infrastructure --startup-project backend/src/InterviewDrill.Api

# Crear una nueva migración (si cambias las entidades)
dotnet ef migrations add NombreDeMigracion --project backend/src/InterviewDrill.Infrastructure --startup-project backend/src/InterviewDrill.Api

# Aplicar migraciones a la base de datos
dotnet ef database update --project backend/src/InterviewDrill.Infrastructure --startup-project backend/src/InterviewDrill.Api

# Revertir última migración
dotnet ef database update MigracionAnterior --project backend/src/InterviewDrill.Infrastructure --startup-project backend/src/InterviewDrill.Api
```

---

## Verification

After creating the database, run:

```powershell
dotnet build backend/InterviewDrill.slnx --nologo
dotnet test backend/InterviewDrill.slnx --nologo
```

- **Build**: 0 errors.
- **Tests**: Core 35/35, Agents 42/42, Api 12/12 and Infrastructure 58/58 = **147/147**. The `InterviewDrill.Infrastructure.Tests` were blocked by the host's **Windows Defender Application Control (Smart App Control)**; after turning that policy off (Windows Security → App & browser control → Smart App Control → Off) they run normally. Anti-repetition/the model is also verified end-to-end via `scripts/e2e-smoke.ps1 -UseDatabase`. See `IMPLEMENTATION_STATUS.md → Problemas Conocidos`.

## E2E against the real database

The smoke script supports an integration mode against SQL Server Express:

```powershell
# Correr el gate E2E en modo BD real (levanta API+frontend y usa la BD)
powershell -ExecutionPolicy Bypass -File scripts/e2e-smoke.ps1 -UseDatabase
```

What `-UseDatabase` verifies:
- `/api/health` reports `persistence = "sql-server"` (otherwise the gate fails).
- It runs **two** interviews with the same `userId`+JD and requires **0 question overlap**, proving that session 1 was persisted in `Questions` and session 2 queried it from SQL Server.

Without the flag, the default mode is in-memory (`persistence = "in-memory"`), for CI/demos without SQL Server. See spec/plan/tasks **017** (`.specify/`).

### Connection string precedence (Option B)

`-UseDatabase` resolves the string as follows:
1. If `INTERVIEWDRILL_CONNECTION_STRING` is set, it is used as-is (for **Azure SQL / AWS RDS** or any other instance). The script prints `usando INTERVIEWDRILL_CONNECTION_STRING del entorno`.
2. If it is **not** set, it automatically *falls back* to `Server=localhost\SQLEXPRESS;Database=InterviewDrill_Testing;Trusted_Connection=True` and shows `fallback localhost\SQLEXPRESS`.

Example against a cloud instance:

```powershell
$env:INTERVIEWDRILL_CONNECTION_STRING = "Server=tcp:mybox.database.windows.net,1433;Database=InterviewDrill_Production;User ID=...;Password=...;Encrypt=True;TrustServerCertificate=False;"
powershell -ExecutionPolicy Bypass -File scripts/e2e-smoke.ps1 -UseDatabase
```
> Make sure the migrations were applied to that DB first (with the same environment variable, `dotnet ef database update`).

---

## Security notes

- ✅ No hardcoded secrets in the code
- ✅ `Trusted_Connection=True` uses Windows authentication (your user `YOUR-MACHINE\your-user`)
- ✅ The production password is an empty placeholder in `appsettings.json`
- For production, set the `INTERVIEWDRILL_CONNECTION_STRING` environment variable to your real Azure/AWS connection string
