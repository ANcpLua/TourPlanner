# CLAUDE.md

## Project
- course: SWEN2 2026
- stack: Angular 21 + ASP.NET Core 10
- type: Tour planning application
- frontend: Angular MVVM with signals
- backend: C# layered architecture (shared with Blazor variant)

## Architecture
- API: HTTP endpoints, transport validation, OpenAPI
- BL: Business rules, orchestration, domain models
- DAL: Persistence (EF Core + PostgreSQL), external services (OpenRouteService)
- Contracts: Shared DTOs between API and frontend
- Tests: NUnit + Moq, integration tests with real PostgreSQL
- Angular: Components, ViewModels, services, routing

## MVVM Rules
- components: input() and output() only, no service injection
- pages: inject ViewModel via inject(XViewModel), own the VM lifecycle
- viewmodels: call services, expose signals, contain UI logic
- services: HTTP-only, no state, no caching
- auth-state: cross-cutting facade, used by guards/interceptor/navbar directly

## Local Development
- database: docker compose up -d postgres
- backend: dotnet watch --project API
- frontend: npm start
- tests-angular: npm test
- tests-dotnet: dotnet test

## Backend Sync
- source-of-truth: Blazor project at ~/TourPlanner
- shared-layers: API, BL, DAL, Contracts
- angular-additions: TourDomain computed properties (PopularityScore, FormattedPopularity, IsChildFriendly, AverageRating)
- after-blazor-changes: sync Angular backend to match

## Key Conventions
- change-detection: ChangeDetectionStrategy.OnPush on all components
- signal-inputs: readonly on all signal inputs/outputs
- template-members: protected visibility
- control-flow: @if/@for (no *ngIf/*ngFor)
- dependency-injection: inject() function (no constructor injection)
- nav-links: ariaCurrentWhenActive="page"
- forms: reactive forms with FormGroup/FormControl
- error-handling: ViewModels set errorMessage signal on catch

## Testing
- framework: Vitest 4 via Angular CLI (npx ng test)
- angular-tests: 250 tests, 27 files
- dotnet-tests: NUnit + Moq, 416 tests, integration tests with real PostgreSQL via Testcontainers
- coverage-target: 95%+ per flag
- vm-tests: TestBed + HttpTestingController + provideHttpClientTesting
- component-tests: fixture.componentRef.setInput() for signal inputs
- cleanup: afterEach(() => httpTesting.verify()) and vi.restoreAllMocks()
- fixtures: Tests/Fixtures/ split by concern (TestConstants, TourTestData, TourLogTestData, TestMocks, HttpTestHelper, PdfAssertions)
- auth-integration: explicit lockout, single DB lookup, 429 on brute force

## CI/CD
- workflow: .github/workflows/coverage.yml
- angular-job: npm ci, npx ng test --coverage, upload to Codecov (angular flag)
- dotnet-job: dotnet restore/build/test with coverlet, upload to Codecov (dotnet flag)
- badges: CI status + Codecov coverage on README
