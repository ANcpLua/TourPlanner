using Autofac;
using BL.Module;
using Mapster;
using MapsterMapper;

namespace Test.BL;

[TestFixture]
public class OrmModuleTests
{
    [Test]
    public void Module_RegistersServices_Correctly()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule(new OrmModule());

        using var container = builder.Build();

        var config = container.Resolve<TypeAdapterConfig>();
        var mapper = container.Resolve<IMapper>();

        Assert.Multiple(() =>
        {
            Assert.That(config, Is.Not.Null);
            Assert.That(mapper, Is.Not.Null);
            Assert.That(mapper, Is.InstanceOf<Mapper>());
        });
    }

    [Test]
    public void Module_LifetimeScopes_WorkCorrectly()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule(new OrmModule());

        using var container = builder.Build();

        var config1 = container.Resolve<TypeAdapterConfig>();
        var config2 = container.Resolve<TypeAdapterConfig>();

        using var scope1 = container.BeginLifetimeScope();
        using var scope2 = container.BeginLifetimeScope();
        var mapper1 = scope1.Resolve<IMapper>();
        var mapper2 = scope2.Resolve<IMapper>();

        Assert.Multiple(() =>
        {
            Assert.That(config1, Is.SameAs(config2), "TypeAdapterConfig should be singleton");
            Assert.That(mapper1, Is.Not.SameAs(mapper2), "Mapper should be per lifetime scope");
        });
    }
}