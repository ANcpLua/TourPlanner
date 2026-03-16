# .NET 9 → .NET 10 Migration

## 1. SDK & TFM

- `global.json`: SDK `10.0.201`, `allowPrerelease: false`
- `Directory.Build.props` created: centralizes `TargetFramework`, `LangVersion preview`, `Nullable`, `ImplicitUsings`
- All `.csproj` files: removed redundant `<TargetFramework>`, `<LangVersion>`, `<Nullable>`, `<ImplicitUsings>` (inherited from Directory.Build.props)

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
- `TourPlannerContext` → `TourPlannerContext(DbContextOptions<TourPlannerContext> options) : DbContext(options)`
- `TourRepository` → `TourRepository(TourPlannerContext dbContext)`
- `TourLogRepository` → `TourLogRepository(TourPlannerContext dbContext)`
- `TourService` → `TourService(ITourRepository tourRepository, IMapper mapper)`
- `TourLogService` → `TourLogService(ITourLogRepository tourLogRepository, IMapper mapper)`
- `FileService` → `FileService(ITourService tourService, IPdfReportService pdfReportService)`
- `BusinessLogicModule` → `BusinessLogicModule(IConfiguration configuration)`
- `PostgreContextModule` → `PostgreContextModule(IConfiguration configuration)`
- `TourController` → `TourController(ITourService tourService, IMapper mapper)`
- `TourLogController` → `TourLogController(ITourLogService tourLogService, IMapper mapper)`
- `FileController` → `FileController(IFileService fileService, ITourService tourService, IMapper mapper)`
- `TryCatchToastWrapper` → `TryCatchToastWrapper(IToastServiceWrapper toastServiceWrapper, ILogger logger)`
- `ToastService` → `ToastService(IToastService toastService)`
- `RouteApiService` → `RouteApiService(HttpClient httpClient, IConfiguration configuration)`

### field keyword (C# 14)
- Already used in `BaseViewModel.IsProcessing`, `TourViewModel.IsFormVisible/SelectedTour/ModalTour/IsMapVisible`
- Already used in `TourLogViewModel.SelectedTourLog/IsLogFormVisible/IsEditing`
- Already used in `SearchViewModel.SearchText/SearchResults`
- Already used in `MapViewModel.FromCity`
- Already used in `ReportViewModel.CurrentReportUrl/SelectedDetailedTourId`

### FrozenDictionary (System.Collections.Frozen)
- `MapViewModel.Coordinates`: `Dictionary<string, (double, double)>` → `FrozenDictionary<string, (double, double)>`
- Eliminates runtime dictionary overhead for read-only city coordinate lookup

### Collection Expressions
- `RouteApiService.FetchRouteDataAsync`: coordinate arrays use `[[from.Longitude, from.Latitude], ...]` syntax

### Expression-bodied Members
- Multiple methods converted to expression-bodied form where single-statement

### Pattern Matching
- `null` checks use `is not null` / `is null` consistently
- `HttpMethod` checks use property pattern `method is { Method: "POST" or "PUT" }`

## 5. Banned API Cleanup

### DateTime.Now → TimeProvider
- `TourLogPersistence.DateTime` default: `DateTime.Now` → `TimeProvider.System.GetUtcNow().UtcDateTime`
- `TourLog.DateTime` default: `DateTime.Now` → `TimeProvider.System.GetUtcNow().UtcDateTime`
- `TourLogViewModel.ShowAddLogForm()`: `DateTime.Now` → `TimeProvider.System.GetUtcNow().UtcDateTime`
- `TourLogViewModel.ResetForm()`: `DateTime.Now` → `TimeProvider.System.GetUtcNow().UtcDateTime`
- Test updated: `TourLogViewModelTests.ShowAddLogForm_WithValidTourId_ShouldCreateNewTourLog`

## 6. CI/CD Pipeline

- `.github/workflows/coverage.yml`: `dotnet-version: '9.0.x'` → `'10.0.x'`, step name updated

## 7. Docker

- `Dockerfile` already on `mcr.microsoft.com/dotnet/sdk:10.0` and `mcr.microsoft.com/dotnet/aspnet:10.0` (migrated in previous commit)

## 8. Test Results

- 396/396 tests passing
- 0 errors, 1 warning (SixLabors.ImageSharp vulnerability advisory — tracked separately)
