using Microsoft.EntityFrameworkCore;
using NiceCleanLib.Models;

namespace NiceCleanLib.Data;

public class NiceCleanDbContext : DbContext
{
    public NiceCleanDbContext(DbContextOptions<NiceCleanDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Pin> Pins { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Users table
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("UserID");
            entity.Property(e => e.Email).HasMaxLength(127).IsRequired();
            entity.Property(e => e.Password).HasMaxLength(127).IsRequired();
            entity.Property(e => e.Age).HasColumnName("Age").IsRequired();
            entity.Property(e => e.Nickname).HasMaxLength(63);
            entity.Property(e => e.NumberOfWalks).HasDefaultValue(0);
            entity.Property(e => e.IsVerified).HasDefaultValue(false);
            
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure Pins table
        modelBuilder.Entity<Pin>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("PinID");
            entity.Property(e => e.UserId).HasColumnName("UserID").IsRequired();
            entity.Property(e => e.CreationDate).HasColumnName("CreationDate").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Severity).HasColumnName("Severity").HasConversion<string>();
            entity.Property(e => e.Radius).HasDefaultValue(100.0);
            entity.Property(e => e.Status).HasColumnName("Status").HasConversion<string>().HasDefaultValue("UNVERIFIED");
            entity.Property(e => e.PollutionType).HasColumnName("Type").HasConversion<string>();
            entity.Property(e => e.Latitude).IsRequired();
            entity.Property(e => e.Longitude).IsRequired();
            entity.Property(e => e.LocationName).HasMaxLength(255);

            // Foreign key relationship
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
