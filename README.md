# TourPlanner (Blazor)

[![Tests and Coverage](https://github.com/ANcpLua/TourPlanner/actions/workflows/coverage.yml/badge.svg)](https://github.com/ANcpLua/TourPlanner/actions/workflows/coverage.yml)
[![codecov](https://codecov.io/gh/ANcpLua/TourPlanner/graph/badge.svg)](https://codecov.io/gh/ANcpLua/TourPlanner)

SWEN2 2026 -- Tour planning application with .NET 10 backend and Blazor WebAssembly frontend.

## Quick Start (Docker)

Requires Docker or OrbStack.

```bash
docker compose up -d
```

| Service  | URL                          |
|----------|------------------------------|
| Frontend | http://localhost:7226         |
| API      | http://localhost:7102         |
| pgAdmin  | http://localhost:5050         |
| Health   | http://localhost:7102/health  |

pgAdmin login: `admin@admin.com` / `admin`

Open the frontend and register an account — all tour data is user-scoped.

To stop:

```bash
docker compose down
```

### Port conflicts

If any port is already in use, copy the example env file and adjust:

```bash
cp .env.example .env
# edit .env, then:
docker compose up -d
```

## Local Development

### Prerequisites

- .NET SDK 10.0
- Docker or OrbStack (for PostgreSQL)

### Steps

1. Start the database:

```bash
docker compose up -d postgres
```

2. Start the API:

```bash
dotnet run --project API/API.csproj
```

3. Start the Blazor client (separate terminal):

```bash
dotnet run --project UI.Client/UI.Client.csproj
```

Open http://localhost:7226, register an account, and log in.

## Build

```bash
dotnet build
```

## Tests

```bash
dotnet run --project Tests/Tests.csproj
```

## Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) for layer ownership rules.

| Project    | Responsibility                        |
|------------|---------------------------------------|
| UI.Client  | Blazor WASM, ViewModels, components   |
| API        | HTTP endpoints, transport validation  |
| BL         | Business rules, orchestration         |
| DAL        | Persistence, external service access  |
| Contracts  | Shared DTOs (including auth models)   |
| Tests      | Unit tests across all layers          |

## Authentication

Cookie-based auth via ASP.NET Core Identity. All tour and log data is scoped to the authenticated user.

| Endpoint              | Description              |
|-----------------------|--------------------------|
| `POST /api/auth/register` | Create account       |
| `POST /api/auth/login`    | Sign in (sets cookie)|
| `POST /api/auth/logout`   | Sign out             |
| `GET  /api/auth/me`       | Current user info    |

All other API endpoints require authentication (return 401 without a valid session cookie).
