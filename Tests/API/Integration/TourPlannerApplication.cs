using DAL.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Tests.API.Integration;

internal sealed class TourPlannerApplication : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(static services =>
        {
            services.RemoveAll<TourPlannerContext>();
            services.RemoveAll<DbContextOptions<TourPlannerContext>>();
            services.AddDbContext<TourPlannerContext>(static options =>
                options.UseInMemoryDatabase("TourPlannerTest"));
        });
    }
}
