 # Architecture

## Projects

| Project | Responsibility |
|---------|---------------|
| `UI.Client` | Blazor WASM, ViewModels, components, pages, navigation, map |
| `API` | Minimal API endpoints, auth (ASP.NET Core Identity), OpenAPI, transport validation |
| `BL` | Business rules, orchestration, PDF reports, import/export |
| `DAL` | EF Core persistence, repositories, external service adapters (OpenRouteService) |
| `Contracts` | Shared DTOs and request/response models for transport only |
| `Tests` | Unit tests across all layers (NUnit + bUnit + Moq) |

## Layer Rules

```
UI.Client  -->  Contracts  <--  API  -->  BL  -->  DAL
                                          |        |
                                          +-> Contracts
```

- `UI.Client` must not reference `BL` or `DAL`
- `API` must not use models from `UI.Client`
- `BL` must not depend on `UI.Client`
- `DAL` must not expose persistence entities outside its boundary
- `Contracts` must not contain business logic, UI logic, or persistence logic

## UI Pattern (MVVM)

- Razor pages are pure templates: `@inject ViewModel`, bind to properties
- `@code` contains only `OnInitializedAsync` + `PropertyChanged` wiring
- ViewModels own state, HTTP calls, error handling
- `BaseViewModel` provides `ExecuteAsync` (busy state + error handling) and `HandleApiRequestAsync` (error handling only)
- ViewModels call `HttpClient` directly — no service abstraction layer

## Auth

Cookie-based via ASP.NET Core Identity. `CookieAuthenticationStateProvider` checks `/api/account/me`. All tour and log data is user-scoped via `IUserContext`.

## Error Handling

- Single catch point per operation (ViewModel layer via `TryCatchToastWrapper`)
- HTTP services throw on failure — no silent swallowing
- `FileService` returns `null`/`bool` for not-found — caller decides HTTP semantics
- Fody decorators log entry/exit timing and arg count (never arg values)
