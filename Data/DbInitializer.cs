using EventEaseApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EventEaseApp.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.Migrate();

            // Check if any venues exist
            if (context.Venues.Any())
            {
                return; // DB has been seeded
            }

            var venues = new Venue[]
            {
                new Venue
                {
                    Id = "v-001",
                    Name = "Grand Horizon Ballroom",
                    Location = "Cape Town City Centre",
                    Capacity = 500,
                    HourlyRate = 1500.00m,
                    ImageUrl = "https://images.unsplash.com/photo-1519167758481-83f550bb49b3?auto=format&fit=crop&w=800&q=80",
                    AmenitiesJson = "[\"WiFi\",\"Projector\",\"Stage\",\"Sound System\",\"Catering Service\"]",
                    Status = "Active",
                    Description = "A luxurious ballroom featuring crystal chandeliers, full sound system, and panoramic ocean views suitable for corporate galas and weddings."
                },
                new Venue
                {
                    Id = "v-002",
                    Name = "Sunset Garden Pavilion",
                    Location = "Stellenbosch Wine Route",
                    Capacity = 250,
                    HourlyRate = 950.00m,
                    ImageUrl = "https://images.unsplash.com/photo-1527529482837-4698179dc6ce?auto=format&fit=crop&w=800&q=80",
                    AmenitiesJson = "[\"Outdoor Lighting\",\"Bar Area\",\"Stage\",\"Parking\"]",
                    Status = "Active",
                    Description = "Stunning outdoor pavilion set amongst vineyard gardens, ideal for cocktail parties, outdoor receptions, and corporate retreats."
                },
                new Venue
                {
                    Id = "v-003",
                    Name = "Apex Innovation Hub",
                    Location = "Sandton Financial District",
                    Capacity = 80,
                    HourlyRate = 600.00m,
                    ImageUrl = "https://images.unsplash.com/photo-1431540015161-0bf868a2d407?auto=format&fit=crop&w=800&q=80",
                    AmenitiesJson = "[\"High-Speed Fiber\",\"Smart Boards\",\"Video Conferencing\"]",
                    Status = "Active",
                    Description = "State-of-the-art auditorium and conference space with high-speed fiber internet and interactive smart displays for tech summits."
                }
            };

            context.Venues.AddRange(venues);
            context.SaveChanges();

            var events = new Event[]
            {
                new Event
                {
                    Id = "e-001",
                    Title = "Tech Innovation Summit 2026",
                    ClientName = "Sarah Jenkins",
                    ClientEmail = "sarah.j@techcorp.co.za",
                    ExpectedAttendance = 75,
                    StartDate = DateTime.Now.AddDays(7),
                    EndDate = DateTime.Now.AddDays(7).AddHours(8),
                    VenueId = "v-003",
                    Status = "Booked"
                },
                new Event
                {
                    Id = "e-002",
                    Title = "Annual Healthcare Leadership Gala",
                    ClientName = "Dr. Michael Vance",
                    ClientEmail = "mvance@medafrica.org",
                    ExpectedAttendance = 450,
                    StartDate = DateTime.Now.AddDays(14),
                    EndDate = DateTime.Now.AddDays(14).AddHours(6),
                    VenueId = "v-001",
                    Status = "Booked"
                },
                new Event
                {
                    Id = "e-003",
                    Title = "Creative Design Expo",
                    ClientName = "Elena Rostova",
                    ClientEmail = "elena@designstudio.io",
                    ExpectedAttendance = 200,
                    StartDate = DateTime.Now.AddDays(21),
                    EndDate = DateTime.Now.AddDays(21).AddHours(6),
                    VenueId = null,
                    Status = "Unassigned"
                }
            };

            context.Events.AddRange(events);
            context.SaveChanges();

            var bookings = new Booking[]
            {
                new Booking
                {
                    Id = "b-001",
                    BookingCode = "BK-A1B2C3D4",
                    VenueId = "v-003",
                    EventId = "e-001",
                    SpecialistName = "David Ross",
                    StartDate = DateTime.Now.AddDays(7),
                    EndDate = DateTime.Now.AddDays(7).AddHours(8),
                    TotalCost = 4800.00m,
                    Status = "Confirmed"
                },
                new Booking
                {
                    Id = "b-002",
                    BookingCode = "BK-E5F6G7H8",
                    VenueId = "v-001",
                    EventId = "e-002",
                    SpecialistName = "Amanda Peterson",
                    StartDate = DateTime.Now.AddDays(14),
                    EndDate = DateTime.Now.AddDays(14).AddHours(6),
                    TotalCost = 9000.00m,
                    Status = "Confirmed"
                }
            };

            context.Bookings.AddRange(bookings);
            context.SaveChanges();
        }
    }
}
