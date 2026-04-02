namespace API.Endpoints;

public static class ApiRoute
{
    public const string ApiBase = "/api";

    public static class Auth
    {
        public const string Base = $"{ApiBase}/auth";
        public const string Register = "register";
        public const string Login = "login";
        public const string Logout = "logout";
        public const string Me = "me";
        public const string RegisterPath = $"{Base}/{Register}";
        public const string LoginPath = $"{Base}/{Login}";
        public const string LogoutPath = $"{Base}/{Logout}";
        public const string MePath = $"{Base}/{Me}";
    }

    public static class Tour
    {
        public const string Base = $"{ApiBase}/tour";
        public const string Search = "search";

        public static string SearchByText(string searchText) => $"{Base}/{Search}/{searchText}";

        public static string ById(Guid id) => $"{Base}/{id}";
    }

    public static class TourLog
    {
        public const string Base = $"{ApiBase}/tourlog";
        public const string ByTour = "bytour";

        public static string ById(Guid id) => $"{Base}/{id}";

        public static string ByTourId(Guid tourId) => $"{Base}/{ByTour}/{tourId}";
    }

    public static class Routes
    {
        public const string Base = $"{ApiBase}/routes";
        public const string Resolve = $"{Base}/resolve";
    }

    public static class Reports
    {
        public const string Base = $"{ApiBase}/reports";
        public const string Summary = $"{Base}/summary";
        public const string TourSummary = $"{Base}/tour";
        public const string Export = $"{Base}/export";
        public const string Import = $"{Base}/import";

        public static string TourById(Guid tourId) => $"{Base}/tour/{tourId}";
        public static string ExportById(Guid tourId) => $"{Base}/export/{tourId}";
    }

    public const string OpenApiDocument = "/openapi/v1.json";

    public const string Health = "/health";
    public const string HealthLive = "/health/live";
    public const string HealthReady = "/health/ready";

    public const string CorsPolicy = "AllowUI";

    public static class HealthChecks
    {
        public const string Self = "self";
        public const string Ready = "ready";
        public const string SelfCheckName = "self";
        public const string ReadyCheckName = "postgres";
    }
}

public static class ApiTag
{
    public const string Auth = "Auth";
    public const string Routes = "Routes";
    public const string Reports = "Reports";
}
