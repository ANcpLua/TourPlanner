# .NET 9 → .NET 10 Migration

## 1. SDK & TFM

- `global.json`: SDK `10.0.201`, `allowPrerelease: false`
- `Directory.Build.props` created: centralizes `TargetFramework`, `LangVersion preview`, `Nullable`, `ImplicitUsings`
- All `.csproj` files: removed redundant properties (inherited from Directory.Build.props)

## 2. Central Package Management (CPM)

- `Directory.Packages.props` created with `ManagePackageVersionsCentrally=true`
- `Version.props` created with all version variables (MSBuild naming convention)
- All `.csproj` files: removed `Version=` attributes from `<PackageReference>` elements
- `System.Text.Json` package reference removed (framework-provided in net10.0)

## 3. NuGet Package Upgrades

| Package | Old | New |
|---------|-----|-----|
| Microsoft.EntityFrameworkCore | 9.0.6 | 10.0.0 |
| Microsoft.EntityFrameworkCore.Design | 9.0.6 | 10.0.0 |
| Microsoft.EntityFrameworkCore.Tools | 9.0.6 | 10.0.0 |
| Microsoft.EntityFrameworkCore.Relational | 9.0.6 | 10.0.0 |
| Microsoft.EntityFrameworkCore.InMemory | 9.0.6 | 10.0.0 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 9.0.4 | 10.0.0 |
| Microsoft.AspNetCore.Components.WebAssembly | 9.0.6 | 10.0.0 |
| Microsoft.AspNetCore.Components.WebAssembly.DevServer | 9.0.6 | 10.0.0 |
| Microsoft.Extensions.Configuration.Abstractions | 9.0.6 | 10.0.0 |
| Microsoft.NET.ILLink.Tasks | 9.0.6 | 10.0.0 |
| Microsoft.NET.Sdk.WebAssembly.Pack | 9.0.6 | 10.0.0 |
| System.Text.Json | 9.0.6 | removed (framework) |

## 4. C# 14 / Modern Language Features

### Primary Constructors (everywhere)
- `TourPlannerContext`, `TourRepository`, `TourLogRepository`
- `TourService`, `TourLogService`, `FileService`
- `BusinessLogicModule`, `PostgreContextModule`
- `TourController`, `TourLogController`, `FileController`
- `TryCatchToastWrapper`, `ToastService`, `RouteApiService`

### field keyword (C# 14)
- Used in `BaseViewModel.IsProcessing`, `TourViewModel`, `TourLogViewModel`, `SearchViewModel`, `MapViewModel`, `ReportViewModel`

### FrozenDictionary (System.Collections.Frozen)
- `MapViewModel.Coordinates`: `Dictionary` → `FrozenDictionary` for immutable city coordinate lookup

### Collection Expressions
- `RouteApiService.FetchRouteDataAsync`: `[[from.Longitude, from.Latitude], ...]` syntax

### Pattern Matching
- `is not null` / `is null` consistently
- `method is { Method: "POST" or "PUT" }` property pattern

## 5. CancellationToken Propagation

Full CancellationToken threading through all async paths:
- `ITourRepository` → `CreateTourAsync`, `UpdateTourAsync`, `DeleteTourAsync` + `CancellationToken`
- `ITourService` → same pattern
- `IFileService.ImportTourFromJsonAsync` + `CancellationToken`
- All controller actions accept `CancellationToken cancellationToken = default`
- `FindAsync([id], cancellationToken)` with params array syntax

## 6. Null Safety

- `ITourService.GetTourById` returns `TourDomain?` (was non-nullable with null! hack)
- `FileService.GenerateTourReport` / `ExportTourToJson`: `?? throw new InvalidOperationException`
- `TourController.GetTourById`: `if (tour is null) return NotFound()`
- `TourLogRepository.DeleteTourLogAsync`: `if (tourLogPersistence is not null)` guard

## 7. Banned API Cleanup

### DateTime.Now → TimeProvider
- `TourLogPersistence.DateTime`: `DateTime.Now` → `TimeProvider.System.GetUtcNow().UtcDateTime`
- `TourLog.DateTime`: same
- `TourLogViewModel.ShowAddLogForm()` / `ResetForm()`: same
- `ReportViewModel.GenerateAndDownloadReport` / `ExportTourToJsonAsync`: `DateTime.UtcNow` → `TimeProvider`

## 8. Analyzer Fixes

- `Directory.Build.props`: `EnableNETAnalyzers`, `AnalysisLevel latest-recommended`, `EnforceCodeStyleInBuild`
- CA1051: `BaseViewModel` public fields → auto-properties
- CA1869: Cached `static readonly JsonSerializerOptions` in `ReportViewModel`
- `.editorconfig`: CA1716 (namespace keywords), CA1710 (Attribute suffix) suppressed (not a public library)
- `using var` on `HttpRequestMessage` (IDisposable)

## 9. Deterministic Seed Data

- `TourPlannerContext.HasData`: `Guid.NewGuid()` → `Guid.Parse("a1b2c3d4-...")` for reproducible migrations

## 10. CI/CD Pipeline

- `.github/workflows/coverage.yml`: `dotnet-version: '9.0.x'` → `'10.0.x'`

## 11. Docker

- `Dockerfile` already on `mcr.microsoft.com/dotnet/sdk:10.0` and `mcr.microsoft.com/dotnet/aspnet:10.0`

## 12. Test Results

- 380/380 tests passing, 0 failures
- 1 warning: SixLabors.ImageSharp vulnerability (NU1902)

## 13. Remaining Test Changes (unstaged)

Test files adapted by linter for CancellationToken signatures and modernized bUnit assertions.
These are functionally correct (380/380 pass) but need manual commit due to assertion refactoring.
