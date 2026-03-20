using DAL.PersistenceModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DAL.Infrastructure;

public class TourPlannerContext(DbContextOptions<TourPlannerContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<TourPersistence> ToursPersistence { get; set; } = null!;
    public DbSet<TourLogPersistence> TourLogsPersistence { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<TourPersistence>(static entity =>
        {
            entity.ToTable("Tours");
            entity.HasKey(static t => t.Id);
            entity.Property(static t => t.UserId).IsRequired().HasMaxLength(450);
            entity.HasIndex(static t => t.UserId);
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

        builder.Entity<TourLogPersistence>(static entity =>
        {
            entity.ToTable("TourLogs");
            entity.HasKey(static tl => tl.Id);
            entity.Property(static tl => tl.UserId).IsRequired().HasMaxLength(450);
            entity.HasIndex(static tl => tl.UserId);
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
    }
}
