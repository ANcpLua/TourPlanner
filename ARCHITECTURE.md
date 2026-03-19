# Architecture

## Projects

- `UI.Client`
- `API`
- `BL`
- `DAL`
- `Contracts`
- `Tests`

## Ownership

- `UI.Client`
  - Views
  - Components
  - ViewModels
  - UI state
  - client-side validation
  - navigation
  - map presentation

- `API`
  - endpoints
  - request validation at HTTP boundary
  - transport mapping
  - OpenAPI
  - auth later if needed

- `BL`
  - business rules
  - use-case logic
  - orchestration
  - report generation coordination
  - import and export rules

- `DAL`
  - persistence
  - database models
  - repositories
  - external service adapters

- `Contracts`
  - DTOs
  - request and response models
  - shared enums and value types for transport only

- `Tests`
  - layer-specific tests

## Rules

- `API` must not reference models from `UI.Client`
- `UI.Client` must not know `BL` or `DAL` directly
- `BL` must not depend on `UI.Client`
- `DAL` must not own UI or HTTP models
- `Contracts` must not contain business logic
