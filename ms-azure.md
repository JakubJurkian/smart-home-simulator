# Smart Home Simulator — Full Project Summary: From Zero to Production

## 🏠 What We Built

A **full-stack Smart Home Simulator** — a web application that lets users register, log in, create rooms, add IoT devices (lights, thermostats, etc.), and control them in real time. The system includes a **React frontend**, a **.NET 8 Web API backend**, a **SQL database**, **real-time SignalR communication**, **TCP server for simulator clients**, **MQTT listener**, and is **deployed to Microsoft Azure**.

---

## 📐 Architecture Overview

```
┌─────────────────────┐       HTTPS        ┌──────────────────────────────┐
│   React Frontend    │ ◄────────────────► │   ASP.NET Core Web API      │
│   (Vite + TS)       │   REST + SignalR    │   (SmartHome.Api)            │
│   Azure Static Web  │                    │   Azure Container App        │
└─────────────────────┘                    │                              │
                                           │  ┌────────────────────────┐  │
                                           │  │ Controllers            │  │
                                           │  │ Background Services    │  │
                                           │  │  - TCP Server          │  │
                                           │  │  - MQTT Listener       │  │
                                           │  │ SignalR Hub            │  │
                                           │  └────────────────────────┘  │
                                           │              │               │
                                           └──────────────┼───────────────┘
                                                          │ EF Core
                                                          ▼
                                           ┌──────────────────────────────┐
                                           │   Azure SQL Database         │
                                           │   (Production)               │
                                           │   SQLite (Development)       │
                                           └──────────────────────────────┘
```

---

## 🧱 Phase 1: Backend Foundation — Domain-Driven Design

### Project Structure (Clean Architecture)

We organized the backend into four projects following **Clean Architecture** principles:

```
backend/
├── SmartHome.Api/              # Presentation layer (Controllers, Hubs, Program.cs)
├── SmartHome.Domain/           # Core business logic (Entities, Interfaces, Enums)
├── SmartHome.Infrastructure/   # Data access (EF Core, Repositories, Services)
└── SmartHome.Tests/            # Unit + Integration tests
```

**Why this structure?** The Domain layer has zero dependencies on anything external. The Infrastructure layer implements the interfaces defined in Domain. The API layer wires everything together. This means you can swap out SQL Server for MongoDB, or swap out the API for a gRPC server, without changing business logic.

### Domain Layer — Entities and Interfaces

We created the core **entity models**:

- **`Device`** — base class using **TPH (Table-Per-Hierarchy) inheritance** with a `Discriminator` column. Properties: `Id`, `Name`, `Type`, `RoomId`, `UserId`.
- **`SmartLight`** — extends `Device`, adds `IsOn` (bool).
- **`Thermostat`** — extends `Device`, adds `CurrentTemperature` (double).
- **`User`** — `Id`, `Username`, `Email`, `PasswordHash`, `Role` (Admin/Regular).
- **`Room`** — `Id`, `Name`, `UserId`, navigation property to `List<Device>`.
- **`MaintenanceLog`** — `Id`, `DeviceId`, `Date`, `Description`, `PerformedBy`.

We defined **repository interfaces** (e.g., `IDeviceRepository`, `IUserRepository`) in Domain so that Infrastructure could implement them — classic **Dependency Inversion Principle**.

We also defined **service interfaces** (e.g., `IDeviceService`) to hold business logic that sits between controllers and repositories.

### Infrastructure Layer — EF Core and Repositories

**`SmartHomeDbContext`** — our Entity Framework Core `DbContext`. It contains `DbSet<Device>`, `DbSet<User>`, `DbSet<Room>`, `DbSet<MaintenanceLog>`. The `OnModelCreating` method configures:

- TPH discriminator for `Device` → `SmartLight` / `Thermostat`
- Relationships (Room has many Devices, User has many Rooms, etc.)
- Required fields, string lengths

**Repositories** — `DeviceRepository`, `UserRepository`, `RoomRepository`, `MaintenanceLogRepository`. Each implements CRUD operations using EF Core. For example, `DeviceRepository.GetAllByUserIdAsync(int userId)` returns all devices owned by a specific user.

**Services** — `DeviceService`, `UserService`, `RoomService`, `MaintenanceLogService`. These contain business logic. For example:

- `UserService.RegisterAsync()` hashes the password with BCrypt before saving
- `UserService.LoginAsync()` verifies the hash and returns the user
- `DeviceService` handles creating the right subtype (`SmartLight` vs `Thermostat`) based on a DTO

### Dual Database Strategy

A key decision: **SQLite for development, SQL Server for production**.

- Locally, you run with `ASPNETCORE_ENVIRONMENT=Development` → SQLite file `smarthome.db`
- On Azure, `ASPNETCORE_ENVIRONMENT=Production` → Azure SQL Server

This is configured in `Program.cs`:

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<SmartHomeDbContext>(options =>
        options.UseSqlite(sqliteConn));
}
else
{
    builder.Services.AddDbContext<SmartHomeDbContext>(options =>
        options.UseSqlServer(sqlServerConn));
}
```

---

## 🔐 Phase 2: Authentication & Authorization

### Cookie-Based Authentication

We chose **cookie authentication** over JWT tokens. Why? Simpler for a web app where frontend and backend share the same trust boundary. The browser automatically sends the cookie with every request.

**How it works:**

1. User calls `POST /api/users/login` with username + password
2. Backend verifies credentials via BCrypt
3. Backend creates a `ClaimsPrincipal` with claims (`UserId`, `Username`, `Role`)
4. Calls `HttpContext.SignInAsync()` which sets a `SmartHomeAuth` cookie
5. Browser sends this cookie with every subsequent request
6. `[Authorize]` attribute on controllers validates the cookie automatically

**Cookie Configuration** was tricky because of cross-origin deployment (frontend on one domain, backend on another):

- `SameSite = None` — required for cross-origin cookies
- `SecurePolicy = Always` — cookies only sent over HTTPS
- `HttpOnly = true` — JavaScript can't access the cookie (XSS protection)

**Custom redirect handling:** By default, ASP.NET redirects to a login page on 401. We overrode this to return raw `401`/`403` status codes since we have a SPA frontend, not server-rendered pages.

### Authorization

Controllers use `[Authorize]` to require authentication. Some endpoints check `User.FindFirst("UserId")` to ensure users can only access their own data. The `GetAllSystemDevices` endpoint has `[AllowAnonymous]` because the simulator background service calls it internally without authentication.

---

## 🌐 Phase 3: API Controllers (REST Endpoints)

### DevicesController

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/devices` | Get all devices for the logged-in user |
| `GET` | `/api/devices/{id}` | Get a specific device |
| `POST` | `/api/devices` | Create a new device (light or thermostat) |
| `PUT` | `/api/devices/{id}` | Update device properties |
| `DELETE` | `/api/devices/{id}` | Remove a device |
| `GET` | `/api/devices/all-system` | **[AllowAnonymous]** Get all devices (for simulator) |

### UsersController

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/users/register` | Create account |
| `POST` | `/api/users/login` | Authenticate and set cookie |
| `POST` | `/api/users/logout` | Clear cookie |
| `GET` | `/api/users/me` | Get current user info (from cookie claims) |

### RoomsController

- Full CRUD for rooms, scoped to the authenticated user

### MaintenanceLogsController

- CRUD for maintenance records, linked to devices

---

## 📡 Phase 4: Real-Time Features

### SignalR Hub

`SmartHomeHub` — a SignalR hub at `/smarthomehub`. When a device state changes (e.g., light turned on, thermostat temperature changes), the backend broadcasts the update to all connected clients in real time.

**`IDeviceNotifier`** interface + **`SignalRNotifier`** implementation — the service layer calls `IDeviceNotifier.NotifyDeviceUpdated(device)` which pushes via SignalR. This decouples SignalR from business logic.

### TCP Smart Home Server

`TcpSmartHomeServer` — a `BackgroundService` that listens on a TCP port. External simulator clients can connect via TCP and send commands (like "turn on light 5" or "set thermostat 3 to 22.5°C"). The server parses these commands, updates the database, and broadcasts changes via SignalR.

### MQTT Listener

`MqttListenerService` — another `BackgroundService` that connects to an MQTT broker. IoT devices in the real world typically communicate via MQTT. This service subscribes to topics and processes incoming device telemetry.

---

## ⚛️ Phase 5: React Frontend

### Tech Stack

- **React 18** with **TypeScript**
- **Vite** as build tool (fast HMR, optimized builds)
- **React Router** for client-side routing
- **Axios** for HTTP requests (with `withCredentials: true` for cookies)
- **SignalR client** (`@microsoft/signalr`) for real-time updates
- **CSS** for styling

### Key Frontend Features

1. **Authentication Flow** — Login/Register forms. On successful login, the cookie is set. `useEffect` on app mount calls `GET /api/users/me` to check if already authenticated.
2. **Dashboard** — Shows all rooms and their devices. Real-time updates via SignalR connection.
3. **Device Control** — Toggle lights on/off, adjust thermostat temperature. Each action calls the REST API, which updates the DB and broadcasts via SignalR.
4. **Room Management** — Create, rename, delete rooms. Assign devices to rooms.
5. **Maintenance Logs** — View and create maintenance records for devices.

### CORS Configuration

The frontend runs on a different origin than the backend (different ports locally, different domains on Azure). We configured CORS in `Program.cs`:

```csharp
policy.WithOrigins(allowedOrigins)
      .AllowAnyMethod()
      .AllowAnyHeader()
      .AllowCredentials();  // Required for cookies!
```

`AllowCredentials()` is critical — without it, the browser won't send cookies cross-origin. But it also means you can't use `AllowAnyOrigin()` — you must specify exact origins.

---

## 🧪 Phase 6: Testing

### Integration Tests

We created integration tests using **`WebApplicationFactory<Program>`** — this spins up the entire ASP.NET pipeline in-memory for testing.

**Key testing decisions:**

- **Environment set to "Testing"** — skips database migrations, TCP server, and MQTT listener
- **In-memory database** — replaced the real DB with `UseInMemoryDatabase()` in tests via a custom `CustomWebApplicationFactory`
- Tests cover: user registration, login, device CRUD, room CRUD, authorization (ensuring users can't access other users' data)

### Unit Tests

Service-level unit tests mock `IDeviceRepository` (using Moq or manual fakes) to test business logic in isolation.

---

## ☁️ Phase 7: Azure Deployment — The Hard Part

This was the most complex phase and where most of the debugging happened.

### Azure Resources Created

1. **Azure Container Registry (ACR)** — stores our Docker images
2. **Azure Container App** — runs the backend API container
3. **Azure SQL Database** — production database (SQL Server)
4. **Azure Static Web App** — hosts the React frontend

### Docker Setup

We created a **multi-stage Dockerfile**:

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish SmartHome.Api/SmartHome.Api.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartHome.Api.dll"]
```

Multi-stage means the final image only contains the runtime + compiled DLLs (~200MB), not the SDK (~800MB).

### Deployment Pipeline

```
Local code → docker build → docker tag → docker push to ACR →
Azure Container App pulls new image → Container restarts
```

Commands we used repeatedly:

```powershell
docker build -t smarthome-api .
docker tag smarthome-api <acr-name>.azurecr.io/smarthome-api:v2
docker push <acr-name>.azurecr.io/smarthome-api:v2
az containerapp update --name <app> --resource-group <rg> --image <acr>/smarthome-api:v2
```

### The Deployment Battles (What Went Wrong)

#### Battle 1: `EnsureCreated()` vs Migrations

**Problem:** `db.Database.EnsureCreated()` checks if the database exists. Azure SQL Database always exists (the server is provisioned), so it returned `true` and **skipped creating tables**. Meanwhile, `db.Database.Migrate()` failed because we had no migration files in the project.

**Solution:** We checked for the actual `Users` table existence using `INFORMATION_SCHEMA.TABLES`. If the table doesn't exist, we drop any stale `__EFMigrationsHistory` table (which tricks `EnsureCreated()` into thinking migrations were applied) and then call `EnsureCreated()`.

#### Battle 2: `Invalid object name 'Devices'`

**Problem:** Even after `EnsureCreated()` seemed to succeed, queries to the `Devices` table failed with SQL Error 208 ("Invalid object name"). This meant the table was never actually created.

**Root cause:** The `__EFMigrationsHistory` table existed from a previous failed deployment. EF Core's `EnsureCreated()` saw this table and assumed the schema was already applied — so it did nothing. But the actual data tables (`Users`, `Devices`, `Rooms`) were never created.

**Solution:** The explicit check-and-drop logic in `Program.cs`:

```csharp
// Check if real tables exist, not just migration history
bool modelTablesExist = db.Database.SqlQueryRaw<int>(
    "SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users'")
    .Single() > 0;

if (!modelTablesExist)
{
    // Drop stale migration history that prevents EnsureCreated from working
    db.Database.ExecuteSqlRaw(
        "IF OBJECT_ID('__EFMigrationsHistory') IS NOT NULL DROP TABLE [__EFMigrationsHistory]");

    db.Database.EnsureCreated();
}
```

#### Battle 3: 401 Unauthorized on `/api/devices/all-system`

**Problem:** The simulator background service (`TcpSmartHomeServer`) calls `GET /api/devices/all-system` internally. But this endpoint required authentication via cookie. The background service doesn't have a browser cookie.

**Solution:** Added `[AllowAnonymous]` attribute to the `GetAllSystemDevices()` action method. This endpoint only returns device states needed by the simulator — no sensitive data.

#### Battle 4: CORS + Cookies Cross-Origin

**Problem:** Frontend on `https://<static-web-app>.azurestaticapps.net` couldn't send cookies to backend on `https://<container-app>.azurecontainerapps.io`. Browser blocked the cookie.

**Solution:** Three things had to align:

1. Backend CORS: `AllowCredentials()` + specific origin (not wildcard)
2. Cookie config: `SameSite=None` + `Secure=Always`
3. Frontend Axios: `withCredentials: true`

All three must be correct simultaneously. If any one is wrong, cookies don't work.

#### Battle 5: Connection String Issues

**Problem:** Azure SQL connection string in Container App environment variables had formatting issues, wrong server name, or missing parameters.

**Solution:** Set the connection string as an environment variable `ConnectionStrings__DefaultConnection` in the Container App settings. The double underscore `__` is the ASP.NET Core convention for nested configuration keys in environment variables (equivalent to `ConnectionStrings:DefaultConnection` in `appsettings.json`).

---

## 🔧 Program.cs — The Brain

The final `Program.cs` is the most important file. Here's what each section does:

### Section 1: Logger

```csharp
builder.Host.UseSerilog(...)
```

Serilog replaces the default .NET logger. Configured via `appsettings.json` to write structured logs. In production, these show up in Azure Container App log streams.

### Section 2: DI Container

Registers all services:

- **`AddDbContext`** — EF Core context with the right provider
- **`AddAuthentication`** — cookie auth scheme
- **`AddSignalR`** — real-time hub
- **`AddHostedService`** — background TCP + MQTT services
- **`AddScoped`** — repositories and services (new instance per HTTP request)
- **`AddCors`** — cross-origin policy

### Section 3: Database Initialization

The battle-tested logic that checks if tables exist and creates them if needed. Runs once at startup.

### Section 4: Middleware Pipeline

Order matters! The pipeline processes requests top to bottom:

1. `UseCors` — handle CORS preflight
2. `UseSerilogRequestLogging` — log every request
3. `UseSwagger` — API docs
4. `UseHttpsRedirection` — force HTTPS
5. `UseCookiePolicy` — cookie consent
6. `UseAuthentication` — parse the cookie, set `HttpContext.User`
7. `UseAuthorization` — check `[Authorize]` attributes
8. `MapControllers` — route to controller actions
9. `MapHub` — SignalR endpoint

---

## 📊 Final Technology Stack

| Layer | Technology |
|-------|-----------|
| Frontend | React 18, TypeScript, Vite, Axios, SignalR Client |
| Backend API | ASP.NET Core 8, C# |
| ORM | Entity Framework Core 8 |
| Auth | Cookie Authentication + BCrypt |
| Real-time | SignalR (WebSockets) |
| IoT Protocols | TCP Server, MQTT Listener |
| Dev Database | SQLite |
| Prod Database | Azure SQL Database |
| Containerization | Docker (multi-stage) |
| Container Registry | Azure Container Registry |
| Backend Hosting | Azure Container Apps |
| Frontend Hosting | Azure Static Web Apps |
| Logging | Serilog (structured logging) |
| Testing | xUnit, WebApplicationFactory, In-Memory DB |

---

## 🎓 Key Lessons Learned

1. **`EnsureCreated()` is not idempotent** — it checks for the database, not the schema. If `__EFMigrationsHistory` exists, it assumes everything is done. For production, use proper migrations or explicit table checks.

2. **Cross-origin cookies require triple alignment** — CORS `AllowCredentials`, cookie `SameSite=None; Secure`, and client `withCredentials: true`. Miss one and it silently fails.

3. **Background services don't have HTTP context** — they can't use cookie auth. Endpoints they call need `[AllowAnonymous]` or a different auth mechanism (API keys, etc.).

4. **Docker multi-stage builds save space** — the build SDK is ~800MB, the runtime is ~200MB. The final image only needs the runtime.

5. **Azure environment variables use `__` for nesting** — `ConnectionStrings__DefaultConnection` maps to `ConnectionStrings:DefaultConnection` in configuration.

6. **Always check the logs first** — Azure Log Stream was our primary debugging tool. Every error we fixed was identified through log output. The structured logging from Serilog made this much easier.

7. **Clean Architecture pays off during deployment** — when we needed to swap SQLite for SQL Server, we only changed the DI registration in `Program.cs`. None of the repository or service code changed.