using Microsoft.EntityFrameworkCore;
using Rezerwacje.Domain.Entities;

namespace Rezerwacje.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Room>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Location).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Reservation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.Room).WithMany(r => r.Reservations).HasForeignKey(x => x.RoomId);
            e.HasOne(x => x.User).WithMany(u => u.Reservations).HasForeignKey(x => x.UserId);
            e.HasIndex(x => new { x.RoomId, x.StartTime, x.EndTime });
        });

        base.OnModelCreating(modelBuilder);
    }
}