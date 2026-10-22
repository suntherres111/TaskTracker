# TaskTracker

[![.NET build and test](https://github.com/suntherres111/TaskTracker/actions/workflows/dotnet.yml/badge.svg)](https://github.com/suntherres111/TaskTracker/actions/workflows/dotnet.yml)

A small Projects and Tasks tracker built as a senior full-stack take-home: an ASP.NET Core 8 Web API
with EF Core and SQL Server, JWT authentication, a Blazor WebAssembly front end, and an external
notification integration designed for Azure Service Bus.

- **API:** projects and tasks CRUD, JWT-protected writes, Swagger UI
- **Web:** login, project list, task list per project, add task, change task status
- **Integration:** `INotificationPublisher`, with a working local implementation and a production design for Azure Service Bus using Managed Identity

---

## Contents

1. [Architecture](#architecture)
2. [Technology stack](#technology-stack)
3. [Prerequisites](#prerequisites)
4. [Setup (copy-paste)](#setup-copy-paste)
5. [Running the API and the web app](#running-the-api-and-the-web-app)
6. [Authentication](#authentication)
7. [API reference and Swagger](#api-reference-and-swagger)
8. [External integration: notifications](#external-integration-notifications)
9. [Tests](#tests)
10. [Configuration reference](#configuration-reference)
11. [Status: completed, limitations, deferred](#status-completed-limitations-deferred)

---

## Architecture

```
TaskTracker.sln
src/
  TaskTracker.Domain/           Entities, enums, notification contract. No package dependencies.
  TaskTracker.Infrastructure/   EF Core (DbContext, mappings, migrations, seed), JWT auth wiring,
                                notification publishers. References Domain.
  TaskTracker.Api/              Controllers, DTOs, application services, Swagger, DI startup.
                                References Infrastructure + Domain.
  TaskTracker.Web/              Blazor WebAssembly UI. No project references; talks to the API over HTTP.
tests/
  TaskTracker.Api.Tests/        xUnit tests for TaskService (SQLite in-memory database).
docs/
  integration-design.md         Azure Service Bus production design for notifications.
```

```
 Browser (Blazor WASM) ──HTTPS + Bearer JWT──▶ TaskTracker.Api ──EF Core──▶ SQL Server / Azure SQL
                                                    │
                                                    └──INotificationPublisher──▶ log (local)
                                                                                 Service Bus (production design)
```

**Why the split:** each project has one concern, and dependencies point inward.

- **Domain** is plain C#: `Project`, `TaskItem`, `TaskItemStatus`, and the
  `INotificationPublisher` contract. It knows nothing about databases, HTTP or Azure.
- **Infrastructure** holds the technical details that may be swapped: EF Core and SQL Server,
  the JWT and password-hash setup, and notification providers. Replacing the local notification
  publisher with Service Bus touches only this project and one DI registration.
- **Api** composes everything. Controllers stay thin: they map HTTP to `ProjectService` and
  `TaskService`, which do the EF queries, DTO mapping and rules. EF entities are never returned
  directly; requests and responses are DTOs.
- **Web** is a separate deployable (static files) that knows only the API's HTTP contract. It has
  its own small copies of the response models rather than referencing server projects.

An Application layer was left out deliberately. At this size the service classes in `Api/Services`
give the same separation without an extra project.

## Technology stack

| Area | Choice |
|---|---|
| Runtime | .NET 8 (LTS) |
| API | ASP.NET Core Web API (controllers), Swashbuckle 6.6 (Swagger) |
| Data | EF Core 8, SQL Server provider. Azure SQL in my environment; any SQL Server works |
| Auth | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`), HS256; PBKDF2 password hash (ASP.NET Core Identity `PasswordHasher`) |
| UI | Blazor WebAssembly (standalone), Bootstrap 5 |
| Secrets | .NET User Secrets locally; app settings or Key Vault in Azure |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A SQL Server database, one of:
  - **Azure SQL Database** (what I used), with your client IP allowed in the server's *Networking* firewall
  - **SQL Server in Docker** (works on any OS, including ARM64):
    ```powershell
    docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<choose-a-strong-password>" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
    ```
  - **LocalDB** (Windows x64 with Visual Studio)
- EF Core CLI tool, matching the EF Core version:
  ```powershell
  dotnet tool install --global dotnet-ef --version 8.0.32
  ```
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`

## Setup (copy-paste)

All commands run from the repository root in **PowerShell**. Use an interactive terminal (Windows
Terminal, or *View → Terminal* in Visual Studio) rather than the Package Manager Console: step 4
prompts for a password with hidden input.

Secrets live in **.NET User Secrets** for the `TaskTracker.Api` project. That is a file in your user
profile (`%APPDATA%\Microsoft\UserSecrets\09d80bef-…\secrets.json`), **outside the repository**.
Nothing secret is committed.

### 1. Restore and build

```powershell
dotnet restore
dotnet build
```

### 2. Database connection string

Read the value without echoing it into your shell history:

```powershell
$cs = Read-Host "Connection string" -MaskInput    # PowerShell 7; in Windows PowerShell 5.1 use plain Read-Host
dotnet user-secrets set "ConnectionStrings:DefaultConnection" $cs --project src/TaskTracker.Api
Remove-Variable cs
```

Example formats (placeholders, not real values):

| Database | Connection string |
|---|---|
| Azure SQL | `Server=tcp:<server>.database.windows.net,1433;Initial Catalog=TaskTrackerDb;User ID=<user>;Password=<password>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;` |
| Azure SQL, passwordless (Entra ID) | `Server=tcp:<server>.database.windows.net,1433;Initial Catalog=TaskTrackerDb;Authentication=Active Directory Default;Encrypt=True;` |
| Docker | `Server=localhost,1433;Database=TaskTrackerDb;User ID=sa;Password=<password>;TrustServerCertificate=True;` |
| LocalDB | `Server=(localdb)\MSSQLLocalDB;Database=TaskTrackerDb;Trusted_Connection=True;` |

You can confirm EF Core picks it up without printing credentials:

```powershell
dotnet ef dbcontext info --project src/TaskTracker.Infrastructure --startup-project src/TaskTracker.Api
```

The output shows the provider, data source and database name only.

### 3. JWT signing key

This generates a random 512-bit key and stores it. The key is never printed and is not saved in
command history:

```powershell
$b = New-Object byte[] 64; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
dotnet user-secrets set "Jwt:SigningKey" ([Convert]::ToBase64String($b)) --project src/TaskTracker.Api
Remove-Variable b
```

Issuer, audience and token lifetime are non-secret and live in `src/TaskTracker.Api/appsettings.json`.

### 4. Demo user

There is one demo account. Only a PBKDF2 hash of its password is stored.

```powershell
dotnet user-secrets set "DemoUser:Username" "demo" --project src/TaskTracker.Api

# Prompts for the password (hidden), then prints a ready-made "dotnet user-secrets set" command. Run that command.
dotnet run --project src/TaskTracker.Api --no-launch-profile -- --hash-password
```

**Test user credentials:** username **`demo`**, plus the password you entered above. For a quick
review, use `TaskTracker2026`. It is a throwaway value for local testing only and protects
nothing real.

Check all four secrets are present (names only, values hidden):

```powershell
dotnet user-secrets list --project src/TaskTracker.Api | ForEach-Object { ($_ -split ' = ')[0] }
```

Expected: `ConnectionStrings:DefaultConnection`, `Jwt:SigningKey`, `DemoUser:Username`, `DemoUser:PasswordHash`.

The API refuses to start, with a clear message, if any of them is missing.

### 5. Create the database schema and seed data

```powershell
dotnet ef database update --project src/TaskTracker.Infrastructure --startup-project src/TaskTracker.Api
```

This applies the single migration, `InitialCreate`, which creates the `Projects` and `Tasks` tables
(FK with cascade delete, index on `ProjectId, Status`) and seeds:

| Project | Tasks |
|---|---|
| 1. Website Redesign | Create wireframes (Done), Build responsive layout (InProgress), Run accessibility audit (Todo) |
| 2. Mobile App Launch | Set up app store listings (Todo), Beta test with internal users (InProgress) |

Seed values use fixed IDs and timestamps, so later migrations don't detect spurious changes.
On Azure SQL serverless, the first connection after an auto-pause can fail with error 40613 while
the database resumes. Wait about a minute and retry. The API itself retries transient errors
automatically (`EnableRetryOnFailure`).

## Running the API and the web app

Use two terminals:

```powershell
# Terminal 1: API (https://localhost:7061, Swagger at /swagger)
dotnet run --project src/TaskTracker.Api --launch-profile https

# Terminal 2: Blazor web app (https://localhost:7167)
dotnet run --project src/TaskTracker.Web --launch-profile https
```

Then open **https://localhost:7167**, sign in with the demo user, and go to **Projects**, then
choose a project to see its tasks. From there you can **add a task** or **change a task's status**.

No emulator is needed. The notification integration runs locally as a logger (see below).

## Authentication

### API

- `POST /api/auth/login` validates the demo credentials against the PBKDF2 hash and returns a JWT:
  `{ accessToken, tokenType: "Bearer", expiresAtUtc }`. The 401 response is identical for an
  unknown user and a wrong password, and the password is checked even when the username is wrong,
  so response timing doesn't reveal valid usernames.
- Tokens are HS256, 60 minutes, with `sub`, `name` and `jti` claims. Signature, issuer, audience
  and lifetime are validated (30-second clock skew); only HS256 is accepted.
- **All writes (POST, PUT, PATCH, DELETE) require a valid token** (`[Authorize]` on the
  controllers). **Reads (GET) are anonymous** (`[AllowAnonymous]`). New endpoints are therefore
  protected unless they explicitly opt out.
- `GET /api/auth/me` is protected; clients use it to confirm a token is still accepted.

### Blazor client: where the token lives (and why)

The web app is **standalone Blazor WebAssembly**: it runs entirely in the browser with no server of
its own. The JWT is kept **only in memory**, inside a C# service (`TokenAuthenticationStateProvider`).

- **Not in `localStorage` or `sessionStorage`:** any script on the page can read those, so an XSS
  bug would leak the token. In-memory .NET state isn't exposed as a simple JavaScript value.
- **Trade-off: refreshing the page, or opening a new tab, signs you out.** For this assessment that
  is the deliberate, secure-by-default choice.
- `BearerTokenHandler` attaches `Authorization: Bearer …` only to requests aimed at the API's base
  address, never to other hosts. If the API answers 401, the session is cleared and the user is sent
  to login with a `returnUrl`.
- The client signs out automatically when the token expires.
- Every page requires login (`[Authorize]` on the `Pages` folder) except `/login`. `returnUrl` only
  accepts local paths, preventing open redirects.

**Production alternative:** a *backend for frontend* (BFF). The Blazor app is hosted by an ASP.NET
Core server that holds the token and gives the browser an HttpOnly, Secure, SameSite cookie. This
survives refreshes and keeps tokens out of the browser entirely, at the cost of a server-side host.

## API reference and Swagger

Swagger UI is available in Development at **https://localhost:7061/swagger**. To call protected
endpoints:

1. Run `POST /api/auth/login`.
2. Copy `accessToken`.
3. Click **Authorize** and paste the token, without the `Bearer` prefix.

`src/TaskTracker.Api/TaskTracker.Api.http` contains the same calls for the Visual Studio / VS Code
HTTP client.

| Method | Route | Auth | Responses |
|---|---|---|---|
| POST | `/api/auth/login` | – | 200, 400, 401 |
| GET | `/api/auth/me` | JWT | 200, 401 |
| GET | `/api/projects` | – | 200 (includes `taskCount`) |
| GET | `/api/projects/{id}` | – | 200, 404 |
| POST | `/api/projects` | JWT | 201 + `Location`, 400, 401 |
| PUT | `/api/projects/{id}` | JWT | 200, 400, 401, 404 |
| DELETE | `/api/projects/{id}` | JWT | 204, 401, 404, **409** if the project still has tasks |
| GET | `/api/projects/{projectId}/tasks` | – | 200, 404 (project) |
| GET | `/api/tasks/{id}` | – | 200, 404 |
| POST | `/api/projects/{projectId}/tasks` | JWT | 201 + `Location`, 400, 401, 404 (project) |
| PUT | `/api/tasks/{id}` | JWT | 200, 400, 401, 404 (title, description, due date) |
| PATCH | `/api/tasks/{id}/status` | JWT | 200, 400, 401, 404 |
| DELETE | `/api/tasks/{id}` | JWT | 204, 401, 404 |

Conventions:

- **Validation** uses DataAnnotations on the request DTOs. `[ApiController]` returns 400
  `ValidationProblemDetails`.
- **Enums** travel as names: `"Todo"`, `"InProgress"`, `"Done"`. Unknown names and raw numbers are rejected.
- **Dates are UTC.** Responses end in `Z`. Input with an offset (`2026-12-01T00:00:00+08:00`) is
  converted to UTC; input without an offset is treated as UTC. The Blazor app always sends an
  explicit offset and displays dates in the browser's time zone.
- **Deleting a project** that still has tasks returns 409 instead of silently cascading.
- **Data access** is async throughout, with `AsNoTracking` and SQL-side projection to DTOs for reads.

## External integration: notifications

Creating a task publishes a `TaskCreatedNotification` through `INotificationPublisher`:

| Piece | Location |
|---|---|
| Contract: `INotificationPublisher`, `TaskCreatedNotification` | `TaskTracker.Domain/Notifications` |
| Local implementation: `LoggingNotificationPublisher` | `TaskTracker.Infrastructure/Notifications` |
| Call site, after the task is saved | `TaskService.CreateAsync` (`TaskTracker.Api/Services`) |
| Registration | `TaskTracker.Api/Program.cs` |

**Runs locally:** the publisher writes one structured log line to the API console, for example:

```
Notification published (local): task.created 3f2c… for task 13 in project 2, status Todo, due none, occurred 2026-10-21T09:15:02Z.
```

It logs identifiers and status only; free-text fields and credentials are never logged.

**Failure behaviour:** publishing is best-effort and time-limited (5 s). If it fails, the error is
logged with the event and task IDs, and the API still returns **201**: the task is never rolled back
because a notification failed. To see this, run the API with a publisher that always fails
(Development only):

```powershell
dotnet run --project src/TaskTracker.Api --launch-profile https -- --Notifications:SimulateFailure=true
```

**Changes in production:** a `ServiceBusNotificationPublisher` in Infrastructure sends to a Service
Bus topic. It authenticates with **Managed Identity** (`DefaultAzureCredential`, plus the *Azure
Service Bus Data Sender* role on the topic), so there are no connection strings. It uses SDK
exponential retries, a **transactional outbox** for guaranteed delivery, `MessageId` de-duplication,
and an Azure Function consumer with a dead-letter queue. Only Infrastructure gains Azure packages.

Full design, configuration sample and role assignment: **[docs/integration-design.md](docs/integration-design.md)**.

## Tests

```powershell
dotnet test
```

`tests/TaskTracker.Api.Tests` covers task creation and the notification contract:

| Test | Verifies |
|---|---|
| `CreateAsync_ValidRequest_PublishesTaskCreatedNotification` | A created task publishes exactly one `task.created` notification with the saved task's IDs, title, status and due date |
| `CreateAsync_WhenPublisherFails_TaskIsStillSavedAndReturned` | A throwing publisher does not fail the call; the task is committed (re-read through a fresh context) |
| `CreateAsync_UnknownProject_ReturnsNullAndDoesNotPublish` | An unknown project returns `null` (mapped to 404) with no notification and no new row |

Each test gets its own **SQLite in-memory** database, built from the real EF Core model including
the seed data, and a fake `INotificationPublisher` that records calls or throws. No SQL Server,
Azure SQL or User Secrets are needed, and tests can run in any order.

GitHub Actions (`.github/workflows/dotnet.yml`) runs the same restore, build (warnings as errors)
and test steps on every push and pull request to `main`. It needs no secrets.

## Configuration reference

| Setting | Where | Secret? | Purpose |
|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | API, User Secrets | **Yes** | SQL Server / Azure SQL |
| `Jwt:SigningKey` | API, User Secrets | **Yes** | HMAC key, at least 32 bytes |
| `DemoUser:Username` | API, User Secrets | No* | Demo login name |
| `DemoUser:PasswordHash` | API, User Secrets | Yes (hash) | PBKDF2 hash from `--hash-password` |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` | `TaskTracker.Api/appsettings.json` | No | Token validation and lifetime |
| `Cors:AllowedOrigins` | `TaskTracker.Api/appsettings.Development.json` | No | Origins allowed to call the API from a browser |
| `ApiBaseUrl` | `TaskTracker.Web/wwwroot/appsettings.json` | No | Where the Blazor app sends API calls |
| `Notifications:SimulateFailure` | command line, Development only | No | Demo of publisher-failure handling |

\* Kept in User Secrets so the whole demo identity is configured in one place.

**Development defaults:**

- **CORS:** the API allows the web app's dev origins, `https://localhost:7167` and
  `http://localhost:5098`, with only the `Authorization` and `Content-Type` headers and the five
  verbs used. No cookies are allowed cross-origin, because the token travels in a header.
- **`ApiBaseUrl`** is `https://localhost:7061/`.

**Production:** set `Cors__AllowedOrigins__0=https://<web-app-host>` on the API, and publish the web
app with an `appsettings.Production.json` (or a deployment-time replacement) pointing `ApiBaseUrl`
at the deployed API. Secrets go in App Service settings or Key Vault references, never in files.

## Status: completed, limitations, deferred

### Completed (all mandatory items)

- [x] Four-project solution with one concern per project (Domain, Infrastructure, Api, Web)
- [x] .NET 8, EF Core 8, SQL Server (Azure SQL); migration plus seed (2 projects, 5 tasks)
- [x] Swagger with JWT **Authorize** support
- [x] CRUD API for projects and tasks, including task DELETE and a PATCH for status
- [x] DTOs, async EF, validation, correct status codes (200/201/204/400/401/404/409)
- [x] JWT authentication; all mutating endpoints protected
- [x] Blazor: login, protected pages, project list, tasks per project, add task, update status
- [x] Token handling in dedicated client services (`AuthService`, `TokenAuthenticationStateProvider`, `BearerTokenHandler`)
- [x] External integration: `INotificationPublisher` + working local publisher + Azure Service Bus / Managed Identity design
- [x] UTC-consistent dates end to end; explicit timezone handling in the UI
- [x] No secrets in the repository
- [x] Unit tests for `TaskService` (xUnit, SQLite in-memory, fake notification publisher)

### Known limitations

- **Refreshing the page signs you out** (in-memory token; see [Authentication](#authentication)).
- **Single demo user** from configuration; no user store, registration or password reset.
- **Best-effort notifications:** a failed publish is logged, not retried (the outbox is designed, not built).
- **Due dates are date-only in the UI**, stored as the start of that day in the user's time zone.
  Seeded due dates are midnight UTC, so in time zones behind UTC they display as the previous day.
- **The UI covers the required flows only:** no project create/edit/delete screens and no task
  edit/delete screens, although the API supports all of them.

### Deferred enhancements (good-to-have list and next steps)

- Role-based authorization (Admin vs User), e.g. a `role` claim and `[Authorize(Roles = "Admin")]` on deletes
- Health checks for SQL and the notification dependency (`/health`)
- Broader test coverage (project service, controllers via `WebApplicationFactory`, `TasksApiClient` with a fake handler)
- Rate limiting on `/api/auth/login` (built-in ASP.NET Core rate limiter)
- Transactional outbox and the Service Bus publisher
- BFF + HttpOnly cookie for the web app; CI pipeline; infrastructure as code (Bicep)
