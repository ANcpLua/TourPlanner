using Autofac;
using BL.Interface;
using BL.Service;

namespace BL.Module;

public class BusinessLogicModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<RouteService>().As<IRouteService>().InstancePerLifetimeScope();
        builder.RegisterType<TourService>().As<ITourService>().InstancePerLifetimeScope();
        builder.RegisterType<TourLogService>().As<ITourLogService>().InstancePerLifetimeScope();
        builder.RegisterType<FileService>().As<IFileService>().InstancePerLifetimeScope();
        builder.RegisterType<PdfReportService>().As<IPdfReportService>().InstancePerLifetimeScope();
    }
}
