using BL.DomainModel;
using DAL.PersistenceModel;
using Mapster;
using UI.Model;

namespace BL.Mapper;

public static class MappingConfiguration
{
    public static TypeAdapterConfig RegisterMapping()
    {
        var config = new TypeAdapterConfig();
        ConfigureTourMappings(config);
        ConfigureTourLogMappings(config);

        return config;
    }

    private static void ConfigureTourMappings(TypeAdapterConfig config)
    {
        config
            .NewConfig<TourPersistence, TourDomain>()
            .Map(static dest => dest.Logs, static src => src.TourLogPersistence);

        config
            .NewConfig<TourDomain, TourPersistence>()
            .Map(static dest => dest.TourLogPersistence, static src => src.Logs);

        config.NewConfig<TourDomain, Tour>().Map(static dest => dest.TourLogs, static src => src.Logs);

        config.NewConfig<Tour, TourDomain>().Map(static dest => dest.Logs, static src => src.TourLogs);
    }

    private static void ConfigureTourLogMappings(TypeAdapterConfig config)
    {
        config
            .NewConfig<TourLogDomain, TourLogPersistence>()
            .Map(static dest => dest.TourPersistenceId, static src => src.TourDomainId);

        config
            .NewConfig<TourLogPersistence, TourLogDomain>()
            .Map(static dest => dest.TourDomainId, static src => src.TourPersistenceId);

        config
            .NewConfig<TourLogDomain, TourLog>()
            .Map(static dest => dest.TourId, static src => src.TourDomainId);

        config
            .NewConfig<TourLog, TourLogDomain>()
            .Map(static dest => dest.TourDomainId, static src => src.TourId);
    }
}
