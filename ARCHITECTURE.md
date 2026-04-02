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

## Client-Agnostic Backend

- The backend owns an HTTP/OpenAPI contract, not a UI-specific integration
- `API`, `BL`, `DAL`, and `Contracts` must remain usable by any client that honors that contract
- `UI.Client` is replaceable; backend code must not assume Blazor-specific state, components, or ViewModels
- Shared transport models belong in `Contracts`; UI state and UI behavior do not

## Model and ViewModel Separation

- Models and ViewModels are different responsibilities and must not be collapsed into the same type
- `Contracts` models describe transport only
- `UI.Client` models describe UI-facing semantics, derived values, parsing, normalization, and mapping from transport to presentation
- `UI.Client` ViewModels own screen state, async workflows, commands, loading flags, selection, and error presentation
- Components and pages must not consume persistence entities or business-layer types directly
- A ViewModel may use a model, but a model must not depend on a ViewModel
- Generated or shared transport DTOs must not become the UI model by default; map them at the boundary when the UI needs its own semantics

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
