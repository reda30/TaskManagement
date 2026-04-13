# Task Management API

A clean, DDD-style Task Management Backend built with **ASP.NET Core 8**, demonstrating RESTful API design, JWT authentication, Redis caching, background processing, and proper separation of concerns.

---

## Table of Contents
- [Project Structure](#project-structure)
- [Architecture Overview](#architecture-overview)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Setup & Running](#setup--running)
  - [Option A – Docker Compose (recommended)](#option-a--docker-compose-recommended)
  - [Option B – Local Development](#option-b--local-development)
- [Seeded Admin Credentials](#seeded-admin-credentials)
- [API Endpoints](#api-endpoints)
- [Authentication Flow](#authentication-flow)
- [Redis Caching](#redis-caching)
- [Background Processing](#background-processing)
- [Business Logic](#business-logic)
- [Running Tests](#running-tests)
- [Assumptions](#assumptions)

---

## Project Structure

```
TaskManagement/
├── src/
│   ├── TaskManagement.Domain/          # Core domain: Entities, Enums, Interfaces, Exceptions
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   └── TaskItem.cs
│   │   ├── Enums/
│   │   │   └── TaskEnums.cs
│   │   ├── Exceptions/
│   │   │   └── DomainExceptions.cs
│   │   └── Interfaces/
│   │       └── IRepositories.cs
│   │
│   ├── TaskManagement.Application/     # Use cases, service interfaces, DTOs
│   │   ├── Interfaces/
│   │   │   └── IServices.cs            # ITokenService, IPasswordHasher, ICacheService, ITaskQueueService
│   │   ├── Users/
│   │   │   ├── DTOs/UserDtos.cs
│   │   │   └── UserService.cs
│   │   └── Tasks/
│   │       ├── DTOs/TaskDtos.cs
│   │       └── TaskService.cs
│   │
│   ├── TaskManagement.Infrastructure/  # EF Core, Redis, JWT, BCrypt, Background Worker
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/
│   │   │   ├── Repositories/
│   │   │   └── Migrations/
│   │   ├── Services/
│   │   │   ├── TokenService.cs
│   │   │   └── BcryptPasswordHasher.cs
│   │   ├── Caching/
│   │   │   └── RedisCacheService.cs
│   │   ├── BackgroundJobs/
│   │   │   └── TaskProcessingWorker.cs
│   │   ├── Seeding/
│   │   │   └── DatabaseSeeder.cs
│   │   └── DependencyInjection.cs
│   │
│   └── TaskManagement.API/             # Controllers, Middleware, Startup
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── AdminUsersController.cs
│       │   └── TasksController.cs
│       ├── Extensions/
│       │   └── ServiceExtensions.cs    # JWT + Swagger registration
│       ├── Middleware/
│       │   └── ExceptionHandlingMiddleware.cs
│       ├── Program.cs
│       ├── appsettings.json
│       └── appsettings.Development.json
│
├── tests/
│   └── TaskManagement.Tests/
│       ├── Users/
│       │   └── UserServiceTests.cs
│       └── Tasks/
│           ├── TaskServiceTests.cs
│           └── EntityTests.cs
│
├── Dockerfile
├── docker-compose.yml
└── README.md
```

---

## Architecture Overview

The solution follows **Domain-Driven Design (DDD)** with a clean layered architecture:

```
┌─────────────────────────────────────────────────────────────┐
│                      API Layer                               │
│   Controllers · Middleware · Swagger · JWT Config           │
├─────────────────────────────────────────────────────────────┤
│                  Application Layer                           │
│   Use Cases (Services) · DTOs · Service Interfaces          │
├─────────────────────────────────────────────────────────────┤
│                    Domain Layer                              │
│   Entities · Enums · Domain Exceptions · Repo Interfaces    │
├─────────────────────────────────────────────────────────────┤
│                Infrastructure Layer                          │
│   EF Core · PostgreSQL · Redis · JWT · BCrypt · Worker      │
└─────────────────────────────────────────────────────────────┘
```

**Key design decisions:**
- The **Domain** layer has zero external dependencies (pure C#).
- The **Application** layer depends only on the Domain and defines service/infrastructure interfaces.
- The **Infrastructure** layer implements those interfaces — EF Core repositories, Redis cache, BCrypt hasher, JWT generator, and the background worker.
- The **API** layer wires everything together and handles HTTP concerns.

---

## Tech Stack

| Concern               | Technology                          |
|-----------------------|-------------------------------------|
| Framework             | ASP.NET Core 8                      |
| ORM                   | Entity Framework Core 8             |
| Database              | SQL Server 2022                       |
| Caching               | Redis 7 via StackExchange.Redis      |
| Authentication        | JWT Bearer (System.IdentityModel)   |
| Password Hashing      | BCrypt.Net-Next (work factor 12)    |
| Background Processing | .NET `BackgroundService` (hosted)   |
| API Documentation     | Swashbuckle / Swagger UI            |
| Tests                 | xUnit · Moq · FluentAssertions      |
| Containers            | Docker + Docker Compose             |

---

## Prerequisites

**Option A (Docker):** Docker Desktop or Docker Engine + Docker Compose

**Option B (Local):**
- .NET 8 SDK
- SQL Server 2019+ (or SQL Server Express / LocalDB) running locally
- Redis 7 running locally

---

## Setup & Running

### Option A – Docker Compose (recommended)

```bash
# Clone the repository
git clone <your-repo-url>
cd TaskManagement

# Start all services (API + PostgreSQL + Redis)
docker compose up --build

# The API will be available at:
#   http://localhost:8080        (Swagger UI at root)
```

The container runs migrations and seeds the admin user automatically on first start.

---

### Option B – Local Development

**1. Configure the connection strings**

Edit `src/TaskManagement.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=TaskManagementDb;User Id=sa;Password=YourPassword@123;TrustServerCertificate=True;",
    "Redis": "localhost:6379"
  },
  "JwtSettings": {
    "Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "TaskManagementAPI",
    "Audience": "TaskManagementClient",
    "ExpiresInMinutes": "60"
  }
}
```

**2. Apply EF Core migrations**

```bash
cd src/TaskManagement.API
dotnet ef database update \
  --project ../TaskManagement.Infrastructure \
  --startup-project .
```

> The migration file is already included. This command creates the tables.

**3. Run the API**

```bash
dotnet run --project src/TaskManagement.API
```

The API starts at `https://localhost:5001` / `http://localhost:5000`.  
Swagger UI is served at the root: `http://localhost:5000`.

---

## Seeded Admin Credentials

| Field    | Value                 |
|----------|-----------------------|
| Email    | `admin@example.com`   |
| Password | `Admin@123`           |
| Role     | `Admin`               |

The admin is created automatically on first application start via `DatabaseSeeder`. The seed is idempotent — it checks before inserting and will never create a duplicate.

---

## API Endpoints

### Authentication

| Method | Endpoint            | Auth     | Description                        |
|--------|---------------------|----------|------------------------------------|
| POST   | `/api/auth/register`| Public   | Register a new user                |
| POST   | `/api/auth/login`   | Public   | Login and receive JWT + refresh    |
| GET    | `/api/auth/profile` | JWT      | Get current user's profile         |

### Admin – User Management *(Admin role required)*

| Method | Endpoint                    | Description              |
|--------|-----------------------------|--------------------------|
| GET    | `/api/admin/users`          | List all users           |
| POST   | `/api/admin/users`          | Create a new user        |
| DELETE | `/api/admin/users/{userId}` | Soft-delete a user       |

### Tasks *(JWT required – own tasks only)*

| Method | Endpoint                         | Description                                    |
|--------|----------------------------------|------------------------------------------------|
| GET    | `/api/tasks`                     | List all tasks (sorted by priority → date)     |
| GET    | `/api/tasks/{taskId}`            | Get task by ID (Redis-cached)                  |
| POST   | `/api/tasks`                     | Create a new task                              |
| PATCH  | `/api/tasks/{taskId}/status`     | Update task status (invalidates Redis cache)   |

### Sample Request Bodies

**Register**
```json
{ "name": "Alice", "email": "alice@example.com", "password": "Password@123" }
```

**Login**
```json
{ "email": "admin@example.com", "password": "Admin@123" }
```

**Create Task**
```json
{ "title": "Design DB schema", "description": "ERD for v1", "priority": 2 }
```
> Priority: `0` = Low, `1` = Medium, `2` = High

**Update Status**
```json
{ "status": 1 }
```
> Status: `0` = Pending, `1` = InProgress, `2` = Done

---

## Authentication Flow

```
Client                              API
  │                                  │
  ├─── POST /api/auth/login ─────────►
  │                                  │── Validate email + BCrypt password
  │                                  │── Generate JWT (60 min) + Refresh token
  ◄──────── { accessToken, ... } ────┤
  │                                  │
  ├─── GET /api/tasks                │
  │    Authorization: Bearer <jwt> ──►
  │                                  │── Validate JWT (issuer, audience, expiry, signature)
  │                                  │── Extract userId from "sub" claim
  │                                  │── Extract role from "role" claim
  ◄──────── 200 OK { tasks } ────────┤
```

**JWT Claims included:**
- `sub` – User GUID
- `email` – User email
- `name` – User display name
- `role` – `User` or `Admin`
- `jti` – Unique token ID

**Admin-only endpoints** are protected with `[Authorize(Roles = "Admin")]`. Regular users attempting to access them receive `403 Forbidden`.

---

## Redis Caching

Redis is used to cache the result of `GET /api/tasks/{taskId}`.

```
First request (cache miss):
  Client → API → Redis (miss) → PostgreSQL → Redis.SET(key, dto, 10min) → Client

Subsequent requests (cache hit):
  Client → API → Redis (hit) → Client   ← no DB query

After status update:
  Client → PATCH /api/tasks/{id}/status → PostgreSQL.UPDATE → Redis.DEL(key) → Client
```

- **Cache key format:** `TaskMgmt:task:{taskId}`
- **TTL:** 10 minutes (absolute expiration)
- **Invalidation:** Explicit key deletion on every status update

---

## Background Processing

When a task is created it is immediately saved to the database and its ID is pushed into an **in-memory `ConcurrentQueue<Guid>`** (the `TaskProcessingQueue` singleton).

A `BackgroundService` (`TaskProcessingWorker`) continuously drains the queue and simulates processing:

```
Task created (status = Pending)
        │
        ▼  ~2 seconds
Task status → InProgress  (saved to DB)
        │
        ▼  ~3 seconds
Task status → Done        (saved to DB)
```

This happens entirely in the background — the `POST /api/tasks` response returns immediately after the task is persisted with status `Pending`. You can observe the transitions by polling `GET /api/tasks/{taskId}` (note that Redis caches the first read; use `PATCH` to reset the cache, or wait for the TTL).

A simple `.NET BackgroundService` was chosen as required. External brokers (RabbitMQ, Azure Service Bus) would be used in production for durability and scalability.

---

## Business Logic

Two pieces of business logic are implemented in the `TaskService`:

**1. Duplicate task prevention**
> A user cannot create two tasks with the same title on the same calendar day (UTC).

```csharp
var isDuplicate = await _taskRepository.ExistsTodayAsync(userId, request.Title, ct);
if (isDuplicate) throw new DuplicateTaskException(request.Title);
```

Returns `409 Conflict` with a descriptive message.

**2. Priority-first sorting**
> `GET /api/tasks` always returns tasks ordered by priority descending (High → Medium → Low), then by creation date ascending within the same priority.

```csharp
tasks.OrderByDescending(t => (int)t.Priority).ThenBy(t => t.CreatedAt)
```

---

## Running Tests

```bash
dotnet test tests/TaskManagement.Tests
```

Tests cover:
- `UserService` – Register, Login, GetProfile (happy path + all failure paths)
- `TaskService` – Create, GetById (cache miss/hit), GetAll (sort order), UpdateStatus
- Domain entities – `TaskItem` and `User` creation and mutation

All tests use **Moq** for dependency mocking and **FluentAssertions** for readable assertions. No database or Redis instance is required to run them.

---

## Assumptions

1. **Refresh tokens** are generated and returned in the login response but not stored server-side (no refresh-token rotation endpoint). In a production system, refresh tokens would be persisted and validated.
2. **Task deletion** is not required by the spec and was not implemented. Tasks belong to users and are cascade-deleted when a user is hard-deleted (soft-delete is used instead, so tasks remain).
3. **Admin tasks** – the admin user can only manage *users*, not tasks. Task ownership remains per-user.
4. **Soft delete** is applied to users. The EF global query filter (`HasQueryFilter(u => !u.IsDeleted)`) automatically excludes soft-deleted users from all queries.
5. **Timestamps** are stored and compared in **UTC**.
6. **Migrations** are applied automatically at startup via `_context.Database.MigrateAsync()` in `DatabaseSeeder`.
7. The **JWT secret** in `appsettings.json` is a placeholder. In production it should be injected via environment variables or a secrets manager (Azure Key Vault, AWS Secrets Manager, etc.).
