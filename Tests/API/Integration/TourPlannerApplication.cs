using DAL.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace Tests.API.Integration;

internal sealed class TourPlannerApplication : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                static d => d.ServiceType == typeof(DbContextOptions<TourPlannerContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<TourPlannerContext>(static options =>
                options.UseInMemoryDatabase("TourPlannerTest"));
        });
    }
}
