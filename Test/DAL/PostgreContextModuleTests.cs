using Autofac;
using DAL.Infrastructure;
using DAL.Interface;
using DAL.Module;
using DAL.Repository;

namespace Test.DAL;

[TestFixture]
public class PostgreContextModuleTests
{
    [Test]
    public void Module_RegistersServices_Correctly()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([
                new KeyValuePair<string, string?>("ConnectionStrings:TourPlannerDatabase",
                    "Host=localhost;Database=test;Username=test;Password=test")
            ])
            .Build();

        var builder = new ContainerBuilder();
        builder.RegisterModule(new PostgreContextModule(config));

        using var container = builder.Build();

        var context = container.Resolve<TourPlannerContext>();
        var tourRepo = container.Resolve<ITourRepository>();
        var logRepo = container.Resolve<ITourLogRepository>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(context, Is.Not.Null);
            Assert.That(tourRepo, Is.Not.Null);
            Assert.That(logRepo, Is.Not.Null);
            Assert.That(tourRepo, Is.InstanceOf<TourRepository>());
            Assert.That(logRepo, Is.InstanceOf<TourLogRepository>());
        }
    }

    [Test]
    public void Module_LifetimeScopes_WorkCorrectly()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([
                new KeyValuePair<string, string?>("ConnectionStrings:TourPlannerDatabase",
                    "Host=localhost;Database=test;Username=test;Password=test")
            ])
            .Build();

        var builder = new ContainerBuilder();
        builder.RegisterModule(new PostgreContextModule(config));

        using var container = builder.Build();

        using var scope1 = container.BeginLifetimeScope();
        using var scope2 = container.BeginLifetimeScope();

        var context1 = scope1.Resolve<TourPlannerContext>();
        var context2 = scope2.Resolve<TourPlannerContext>();
        var repo1 = scope1.Resolve<ITourRepository>();
        var repo2 = scope2.Resolve<ITourRepository>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(context1, Is.Not.SameAs(context2), "Context should be per lifetime scope");
            Assert.That(repo1, Is.Not.SameAs(repo2), "Repository should be per lifetime scope");
        }
    }

    [Test]
    public void Module_WithNullConnectionString_StillRegistersServices()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        var builder = new ContainerBuilder();
        builder.RegisterModule(new PostgreContextModule(config));

        using var container = builder.Build();

        var context = container.Resolve<TourPlannerContext>();
        var tourRepo = container.Resolve<ITourRepository>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(context, Is.Not.Null);
            Assert.That(tourRepo, Is.Not.Null);
        }
    }
}