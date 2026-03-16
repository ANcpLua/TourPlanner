using DAL.PersistenceModel;
using Microsoft.EntityFrameworkCore;

namespace DAL.Infrastructure;

public class TourPlannerContext(DbContextOptions<TourPlannerContext> options) : DbContext(options)
{
    public DbSet<TourPersistence> ToursPersistence { get; set; } = null!;
    public DbSet<TourLogPersistence> TourLogsPersistence { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TourPersistence>(static entity =>
        {
            entity.ToTable("Tours");
            entity.HasKey(static t => t.Id);
            entity.Property(static t => t.Name).IsRequired().HasMaxLength(200);
            entity.Property(static t => t.Description).IsRequired().HasMaxLength(500);
            entity.Property(static t => t.From).IsRequired().HasMaxLength(100);
            entity.Property(static t => t.To).IsRequired().HasMaxLength(100);
            entity.Property(static t => t.Distance).HasColumnType("decimal(18,2)");
            entity.Property(static t => t.EstimatedTime);
            entity.Property(static t => t.TransportType).HasMaxLength(50);
            entity.Property(static t => t.ImagePath).HasMaxLength(10000);
            entity.Property(static t => t.RouteInformation).HasMaxLength(30000);

            entity
                .HasMany(static t => t.TourLogPersistence)
                .WithOne(static tl => tl.TourPersistence)
                .HasForeignKey(static tl => tl.TourPersistenceId);
        });

        modelBuilder.Entity<TourLogPersistence>(static entity =>
        {
            entity.ToTable("TourLogs");
            entity.HasKey(static tl => tl.Id);
            entity
                .Property(static tl => tl.DateTime)
                .HasConversion(
                    static v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
                    static v => v.ToUniversalTime()
                );
            entity.Property(static tl => tl.Comment).IsRequired().HasMaxLength(500);
            entity.Property(static tl => tl.Difficulty);
            entity.Property(static tl => tl.Rating);
            entity.Property(static tl => tl.TotalDistance).HasColumnType("decimal(18,2)");
            entity.Property(static tl => tl.TotalTime);

            entity
                .HasOne(static tl => tl.TourPersistence)
                .WithMany(static t => t.TourLogPersistence)
                .HasForeignKey(static tl => tl.TourPersistenceId);
        });

        modelBuilder.Entity<TourPersistence>().HasData(
            new TourPersistence
            {
                Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                Name = "Sample Tour",
                Description = "A sample tour for testing",
                From = "Vienna",
                To = "Salzburg",
                TransportType = "DRIVING"
            }
        );
    }
}