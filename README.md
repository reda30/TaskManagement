# Task Management API

A clean, DDD-style Task Management Backend built with **ASP.NET Core 8**, demonstrating RESTful API design, JWT authentication, Redis caching, background processing, and proper separation of concerns.

---

## Table of Contents
- [Project Structure](#project-structure)
- [Architecture Overview](#architecture-overview)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Setup & Running](#setup--running)
- [Seeded Admin Credentials](#seeded-admin-credentials)
- [API Endpoints](#api-endpoints)
- [Authentication Flow](#authentication-flow)
- [Redis Caching](#redis-caching)
- [Background Processing](#background-processing)
- [Business Logic](#business-logic)
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
│   │   │   └── IServices.cs
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
│       │   └── ServiceExtensions.cs
│       ├── Middleware/
│       │   └── ExceptionHandlingMiddleware.cs
│       ├── Program.cs
│       ├── appsettings.json
│       └── appsettings.Development.json
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
│   EF Core · SQL Server · Redis · JWT · BCrypt · Worker      │
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
| Database              | SQL Server                          |
| Caching               | Redis 7 via StackExchange.Redis     |
| Authentication        | JWT Bearer (System.IdentityModel)   |
| Password Hashing      | BCrypt.Net-Next (work factor 12)    |
| Background Processing | .NET `BackgroundService` (hosted)   |
| API Documentation     | Swashbuckle / Swagger UI            |

---

## Prerequisites

- .NET 8 SDK
- SQL Server (any edition — Express, Developer, LocalDB, or full)
- Redis running locally

> **Install Redis on Windows:** Download from https://github.com/microsoftarchive/redis/releases or run via WSL2 with `sudo service redis-server start`.

---

## Setup & Running

**1. Configure the connection string**

Open `src/TaskManagement.API/appsettings.Development.json` and set the connection string that matches your SQL Server setup:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=TaskManagementDb;Trusted_Connection=True;TrustServerCertificate=True;",
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

Common connection string options depending on your setup:

| Setup | Connection String |
|---|---|
| LocalDB (Visual Studio default) | `Server=(localdb)\\MSSQLLocalDB;Database=TaskManagementDb;Trusted_Connection=True;TrustServerCertificate=True;` |
| SQL Server Express | `Server=.\\SQLEXPRESS;Database=TaskManagementDb;Trusted_Connection=True;TrustServerCertificate=True;` |
| SQL Server default instance | `Server=.;Database=TaskManagementDb;Trusted_Connection=True;TrustServerCertificate=True;` |
| SQL Server with sa login | `Server=localhost,1433;Database=TaskManagementDb;User Id=sa;Password=YourPassword@123;TrustServerCertificate=True;` |

**2. Run the project**

```bash
dotnet run --project src/TaskManagement.API
```

On first startup the application will:
- Automatically apply the EF Core migration (creates the database and tables)
- Seed the default admin user

Swagger UI will be available at: `http://localhost:5000`

---

## Seeded Admin Credentials

| Field    | Value               |
|----------|---------------------|
| Email    | `admin@example.com` |
| Password | `Admin@123`         |
| Role     | `Admin`             |

The admin is created automatically on first run. The seed is idempotent — it checks before inserting and will never create a duplicate.

---

## API Endpoints

### Authentication

| Method | Endpoint             | Auth  | Description                     |
|--------|----------------------|-------|---------------------------------|
| POST   | `/api/auth/register` | Public | Register a new user            |
| POST   | `/api/auth/login`    | Public | Login and receive a JWT token  |
| GET    | `/api/auth/profile`  | JWT   | Get current user's profile      |

### Admin – User Management *(Admin role required)*

| Method | Endpoint                     | Description         |
|--------|------------------------------|---------------------|
| GET    | `/api/admin/users`           | List all users      |
| POST   | `/api/admin/users`           | Create a new user   |
| DELETE | `/api/admin/users/{userId}`  | Soft-delete a user  |

### Tasks *(JWT required – own tasks only)*

| Method | Endpoint                      | Description                                  |
|--------|-------------------------------|----------------------------------------------|
| GET    | `/api/tasks`                  | List all tasks (sorted by priority → date)   |
| GET    | `/api/tasks/{taskId}`         | Get task by ID (Redis-cached)                |
| POST   | `/api/tasks`                  | Create a new task                            |
| PATCH  | `/api/tasks/{taskId}/status`  | Update task status (invalidates Redis cache) |

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

**Admin-only endpoints** are protected with `[Authorize(Roles = "Admin")]`. Regular users receive `403 Forbidden`.

---

## Redis Caching

Redis caches the result of `GET /api/tasks/{taskId}`.

```
First request (cache miss):
  Client → API → Redis (miss) → SQL Server → Redis.SET(key, data, 10min) → Client

Subsequent requests (cache hit):
  Client → API → Redis (hit) → Client   ← no DB query

After status update:
  Client → PATCH /api/tasks/{id}/status → SQL Server UPDATE → Redis.DEL(key) → Client
```

- **Cache key:** `TaskMgmt:task:{taskId}`
- **TTL:** 10 minutes
- **Invalidation:** Cache key is deleted on every status update

---

## Background Processing

When a task is created, its ID is pushed into an in-memory queue. A `BackgroundService` running alongside the API continuously drains the queue and simulates processing:

```
POST /api/tasks → Task saved (Pending) → ID pushed to queue → response returns to user

Background (separate thread):
  ~2 seconds → status updated to InProgress
  ~3 seconds → status updated to Done
```

The HTTP response returns immediately — the status transitions happen asynchronously. You can watch them by calling `GET /api/tasks/{taskId}` a few seconds after creation.

---

## Business Logic

**1. Duplicate task prevention**
A user cannot create two tasks with the same title on the same day. Returns `409 Conflict`.

**2. Priority-first sorting**
`GET /api/tasks` returns tasks sorted by priority descending (High → Medium → Low), then by creation date ascending within the same priority level.

---

## Assumptions

1. **Refresh tokens** are generated and returned in the login response but not stored server-side. In production they would be persisted and validated.
2. **Task deletion** was not implemented as it is not in the spec. Tasks are cascade-deleted only when their owner user is permanently removed.
3. **Admin tasks** — the admin can only manage users, not tasks. Task ownership is always per-user.
4. **Soft delete** is applied to users via an `IsDeleted` flag. A global EF query filter automatically excludes soft-deleted users from all queries.
5. **Timestamps** are stored and compared in UTC.
6. The **JWT secret** in `appsettings.json` is a placeholder and should be replaced with a strong secret stored in environment variables or a secrets manager in production.
