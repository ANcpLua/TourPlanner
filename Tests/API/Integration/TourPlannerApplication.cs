using DAL.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Tests.API.Integration;

internal sealed class TourPlannerApplication : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(static services =>
        {
            services.RemoveAll<DbContextOptions<TourPlannerContext>>();
            services.RemoveAll<TourPlannerContext>();
            services.AddDbContext<TourPlannerContext>(static options =>
                options.UseInMemoryDatabase("TourPlannerTest"));
        });
    }
}
