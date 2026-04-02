using Autofac;
using Autofac.Extensions.DependencyInjection;
using API.Endpoints;
using API.Infrastructure;
using API.Extensions;
using BL.Interface;
using BL.Module;
using DAL.Infrastructure;
using DAL.Module;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration).ReadFrom.Services(services)
);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.RegisterModule(new PostgreContextModule(builder.Configuration));
    containerBuilder.RegisterModule(new BusinessLogicModule());
    containerBuilder.RegisterModule(new OrmModule());
});
builder.Services.AddCors(options =>
{
    options.AddPolicy(ApiRoute.CorsPolicy, policy =>
        policy
            .WithOrigins(
                "http://localhost:7226",
                "http://localhost",
                "http://tourplanner-ui",
                "http://tourplanner-ui:80"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    );
});
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddControllers();
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "TourPlanner API";
        document.Info.Version = "v1";
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["cookie"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Cookie,
            Name = ".AspNetCore.Identity.Application"
        };
        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, _) =>
    {
        if (context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAllowAnonymous>().Any())
            return Task.CompletedTask;

        operation.Security = [new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("cookie")] = []
        }];
        return Task.CompletedTask;
    });
});
builder.Services.AddHttpClient("OpenRouteService").AddStandardResilienceHandler();
builder.Services.AddHealthChecks()
    .AddCheck(
        ApiRoute.HealthChecks.SelfCheckName,
        () => HealthCheckResult.Healthy("Self check passed."),
        tags: [HealthEndpointExtensions.SelfTag]
    )
    .AddCheck<PostgreSqlHealthCheck>(
        ApiRoute.HealthChecks.ReadyCheckName,
        tags: [HealthEndpointExtensions.ReadyTag]
    );

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<TourPlannerContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, HttpUserContext>();

var app = builder.Build();

 


app.UseRouting();
app.UseCors(ApiRoute.CorsPolicy);
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.MapControllers();
app.MapAuthEndpoints();
app.MapRouteEndpoints();
app.MapReportEndpoints();
app.MapHealthEndpoints();
app.MapOpenApi(ApiRoute.OpenApiDocument).AllowAnonymous();
app.Run();
