using CampusRent.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusRent.Server.Data;

public class CampusRentDbContext : DbContext
{
    public CampusRentDbContext(DbContextOptions<CampusRentDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<University> Universities => Set<University>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomImage> RoomImages => Set<RoomImage>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Favorite>().HasKey(f => new { f.UserId, f.RoomId });
        modelBuilder.Entity<Booking>().HasIndex(b => new { b.StudentId, b.RoomId, b.VisitAt }).IsUnique();
        modelBuilder.Entity<Room>().Property(r => r.Amenities).HasConversion(
            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
            v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                (a, b) => a!.SequenceEqual(b!), v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())), v => v.ToList()));

        // User indexes & constraints
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Role).HasConversion<string>();
        });

        // Room relations
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasOne(r => r.Landlord)
                  .WithMany(u => u.Rooms)
                  .HasForeignKey(r => r.LandlordId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.University)
                  .WithMany(u => u.Rooms)
                  .HasForeignKey(r => r.UniversityId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.Property(r => r.Status).HasConversion<string>();
            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => new { r.Latitude, r.Longitude });
        });

        // Review relations & constraints
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasOne(rv => rv.Room)
                  .WithMany(r => r.Reviews)
                  .HasForeignKey(rv => rv.RoomId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rv => rv.User)
                  .WithMany(u => u.Reviews)
                  .HasForeignKey(rv => rv.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(rv => new { rv.UserId, rv.RoomId }).IsUnique();
        });

        // Report relations & constraints
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasOne(rp => rp.Reporter)
                  .WithMany(u => u.Reports)
                  .HasForeignKey(rp => rp.ReporterId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Room)
                  .WithMany(r => r.Reports)
                  .HasForeignKey(rp => rp.RoomId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Review)
                  .WithMany(rv => rv.Reports)
                  .HasForeignKey(rp => rp.ReviewId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(rp => rp.Status).HasConversion<string>();
        });
    }
}
