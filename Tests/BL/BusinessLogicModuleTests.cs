using Autofac;
using BL.Interface;
using BL.Module;
using DAL.Interface;
using MapsterMapper;

namespace Tests.BL;

[TestFixture]
public sealed class BusinessLogicModuleTests
{
    [Test]
    public void Module_Load_RegistersAllCoreServices()
    {
        using var container = BuildContainer();
        using var scope = container.BeginLifetimeScope();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(scope.Resolve<IRouteService>(), Is.Not.Null);
            Assert.That(scope.Resolve<ITourService>(), Is.Not.Null);
            Assert.That(scope.Resolve<ITourLogService>(), Is.Not.Null);
            Assert.That(scope.Resolve<IFileService>(), Is.Not.Null);
            Assert.That(scope.Resolve<IPdfReportService>(), Is.Not.Null);
        }
    }

    [Test]
    public void Module_Load_RegistersServicesPerLifetimeScope()
    {
        using var container = BuildContainer();
        using var firstScope = container.BeginLifetimeScope();
        using var secondScope = container.BeginLifetimeScope();

        var firstScopeTourService = firstScope.Resolve<ITourService>();
        var firstScopeTourServiceAgain = firstScope.Resolve<ITourService>();
        var secondScopeTourService = secondScope.Resolve<ITourService>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstScopeTourService, Is.SameAs(firstScopeTourServiceAgain));
            Assert.That(firstScopeTourService, Is.Not.SameAs(secondScopeTourService));
        }
    }

    private static Autofac.IContainer BuildContainer()
    {
        var configuration = new ConfigurationBuilder().Build();
        var builder = new ContainerBuilder();
        builder.RegisterInstance(configuration).As<IConfiguration>();
        builder.RegisterInstance(Mock.Of<ITourRepository>()).As<ITourRepository>();
        builder.RegisterInstance(Mock.Of<ITourLogRepository>()).As<ITourLogRepository>();
        builder.RegisterInstance(Mock.Of<IRouteRepository>()).As<IRouteRepository>();
        builder.RegisterInstance(Mock.Of<IMapper>()).As<IMapper>();
        builder.RegisterInstance(Mock.Of<IUserContext>()).As<IUserContext>();
        builder.RegisterModule(new BusinessLogicModule());
        return builder.Build();
    }
}
