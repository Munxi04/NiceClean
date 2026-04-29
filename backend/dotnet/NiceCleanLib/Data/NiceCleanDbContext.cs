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
    public DbSet<PinVote> PinVotes { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Participation> Participations { get; set; }

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

        // Configure PinVotes table
        modelBuilder.Entity<PinVote>(entity =>
        {
            entity.HasKey(e => e.Id);

            // This is the magic rule to prevent duplicate votes per pin by the same user:
            entity.HasIndex(e => new { e.PinId, e.UserId }).IsUnique();

            entity.Property(e => e.VoteType).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Foreign key config
            entity.HasOne<Pin>()
                .WithMany()
                .HasForeignKey(e => e.PinId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Deleting a user deletes their votes
        });

        // --> Configure Event table (Added)
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventStatus).HasConversion<string>();
        });

        // --> Configure Participation table
        modelBuilder.Entity<Participation>(entity =>
        {
            entity.HasKey(e => e.ParticipationId);

            // Prevent user from joining the same event twice at the DB layer
            entity.HasIndex(e => new { e.EventId, e.UserId }).IsUnique();

            entity.Property(e => e.JoinDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne<Event>()
                .WithMany()
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --> Configure Report table
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.ReportId);
            entity.Property(e => e.BagVolume).HasConversion<string>();

            // An event can only have one report total.
            entity.HasIndex(e => e.EventId).IsUnique();

            entity.HasOne<Event>()
                .WithMany()
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
