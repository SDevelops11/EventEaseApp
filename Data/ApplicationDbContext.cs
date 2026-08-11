using EventEaseApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EventEaseApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
            : base(options) { }

        public DbSet<Venue> Venues { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Venue -> Events (1:N, Nullable FK)
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Venue)
                .WithMany(v => v.Events)
                .HasForeignKey(e => e.VenueId)
                .OnDelete(DeleteBehavior.SetNull);

            // 2. Venue -> Bookings (1:N, Restrict Delete)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Venue)
                .WithMany(v => v.Bookings)
                .HasForeignKey(b => b.VenueId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Event -> Booking (1:1, Unique Constraint, Restrict Delete)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Event)
                .WithOne(e => e.Booking)
                .HasForeignKey<Booking>(b => b.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.EventId)
                .IsUnique();
        }
    }
}