using Autofac;
using BL.Interface;
using BL.Module;
using DAL.Interface;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Test.BL;

[TestFixture]
public class BusinessLogicModuleTests
{
    [Test]
    public void Module_Load_ExecutesSuccessfully()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([
                new KeyValuePair<string, string?>("AppSettings:ImageBasePath", "/test/images")
            ])
            .Build();

        var builder = new ContainerBuilder();
        builder.RegisterInstance(config).As<IConfiguration>();
        builder.RegisterInstance(Mock.Of<ITourRepository>()).As<ITourRepository>();
        builder.RegisterInstance(Mock.Of<ITourLogRepository>()).As<ITourLogRepository>();
        builder.RegisterInstance(Mock.Of<IMapper>()).As<IMapper>();

        builder.RegisterModule(new BusinessLogicModule(config));

        using var container = builder.Build();
        var service = container.Resolve<IPdfReportService>();

        Assert.That(service, Is.Not.Null);
    }
}