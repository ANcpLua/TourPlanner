using Autofac;
using Autofac.Extensions.DependencyInjection;
using API.Endpoints;
using API.Infrastructure;
using BL.Interface;
using BL.Module;
using DAL.Infrastructure;
using DAL.Module;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration).ReadFrom.Services(services)
);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.RegisterModule(new PostgreContextModule(builder.Configuration));
    containerBuilder.RegisterModule(new BusinessLogicModule(builder.Configuration));
    containerBuilder.RegisterModule(new OrmModule());
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUI", policy =>
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
builder.Services.AddOpenApi();
builder.Services.AddHttpClient("OpenRouteService").AddStandardResilienceHandler();
builder.Services.AddHealthChecks().AddCheck<PostgreSqlHealthCheck>("postgres");

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TourPlannerContext>();
    db.Database.Migrate();
}

app.UseRouting();
app.UseCors("AllowUI");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.MapControllers();
app.MapAuthEndpoints();
app.MapRouteEndpoints();
app.MapReportEndpoints();
app.MapHealthChecks("/health");
app.MapOpenApi("/openapi/{documentName}.json");
app.Run();
