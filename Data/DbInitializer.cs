using EventEaseApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EventEaseApp.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            try
            {
                context.Database.Migrate();
            }
            catch (Exception)
            {
                context.Database.EnsureCreated();
            }

            // Ensure schema updates are applied safely if running against Azure SQL directly
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    -- 1. Ensure dbo_EventTypes table exists
                    IF OBJECT_ID('dbo_EventTypes', 'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo_EventTypes (
                            Id NVARCHAR(450) NOT NULL PRIMARY KEY,
                            Name NVARCHAR(100) NOT NULL,
                            Description NVARCHAR(250) NULL,
                            Icon NVARCHAR(50) NULL
                        );
                    END

                    -- 2. Ensure ImageUrl column exists in dbo_Events
                    IF OBJECT_ID('dbo_Events', 'U') IS NOT NULL AND NOT EXISTS (
                        SELECT * FROM sys.columns 
                        WHERE object_id = OBJECT_ID('dbo_Events') AND name = 'ImageUrl'
                    )
                    BEGIN
                        ALTER TABLE dbo_Events ADD ImageUrl NVARCHAR(MAX) NULL;
                    END

                    -- 3. Ensure EventTypeId column exists in dbo_Events
                    IF OBJECT_ID('dbo_Events', 'U') IS NOT NULL AND NOT EXISTS (
                        SELECT * FROM sys.columns 
                        WHERE object_id = OBJECT_ID('dbo_Events') AND name = 'EventTypeId'
                    )
                    BEGIN
                        ALTER TABLE dbo_Events ADD EventTypeId NVARCHAR(450) NULL;
                    END

                    -- 4. Ensure IsAvailable column exists in dbo_Venues
                    IF OBJECT_ID('dbo_Venues', 'U') IS NOT NULL AND NOT EXISTS (
                        SELECT * FROM sys.columns 
                        WHERE object_id = OBJECT_ID('dbo_Venues') AND name = 'IsAvailable'
                    )
                    BEGIN
                        ALTER TABLE dbo_Venues ADD IsAvailable BIT NOT NULL CONSTRAINT DF_dbo_Venues_IsAvailable DEFAULT 1;
                    END
                ");
            }
            catch
            {
                // Fallback safe ignore
            }

            // Seed or ensure Event Types exist
            if (!context.EventTypes.Any())
            {
                var eventTypes = new EventType[]
                {
                    new EventType
                    {
                        Id = "et-001",
                        Name = "Conference",
                        Description = "Corporate summits, annual general meetings, and professional conventions.",
                        Icon = "bi-building-fill"
                    },
                    new EventType
                    {
                        Id = "et-002",
                        Name = "Wedding",
                        Description = "Wedding ceremonies, banquets, and reception celebrations.",
                        Icon = "bi-heart-fill"
                    },
                    new EventType
                    {
                        Id = "et-003",
                        Name = "Concert",
                        Description = "Live music performances, orchestra recitals, and outdoor festivals.",
                        Icon = "bi-music-note-beamed"
                    },
                    new EventType
                    {
                        Id = "et-004",
                        Name = "Workshop",
                        Description = "Interactive training sessions, hands-on masterclasses, and bootcamps.",
                        Icon = "bi-tools"
                    },
                    new EventType
                    {
                        Id = "et-005",
                        Name = "Gala Dinner",
                        Description = "Charity galas, awards ceremonies, and formal banquets.",
                        Icon = "bi-trophy-fill"
                    },
                    new EventType
                    {
                        Id = "et-006",
                        Name = "Exhibition",
                        Description = "Trade shows, art galleries, and product showcase expos.",
                        Icon = "bi-easel-fill"
                    },
                    new EventType
                    {
                        Id = "et-007",
                        Name = "Seminar",
                        Description = "Educational lectures, academic symposiums, and keynotes.",
                        Icon = "bi-mortarboard-fill"
                    }
                };

                context.EventTypes.AddRange(eventTypes);
                context.SaveChanges();
            }

            // Update any existing events that have no EventTypeId
            var existingEventsWithoutType = context.Events.Where(e => e.EventTypeId == null).ToList();
            if (existingEventsWithoutType.Any())
            {
                var confType = context.EventTypes.FirstOrDefault(et => et.Name == "Conference");
                var galaType = context.EventTypes.FirstOrDefault(et => et.Name == "Gala Dinner");
                var expoType = context.EventTypes.FirstOrDefault(et => et.Name == "Exhibition");
                var defaultType = confType ?? context.EventTypes.FirstOrDefault();

                foreach (var ev in existingEventsWithoutType)
                {
                    if (ev.Title.Contains("Summit", StringComparison.OrdinalIgnoreCase) || ev.Title.Contains("Tech", StringComparison.OrdinalIgnoreCase))
                    {
                        ev.EventTypeId = confType?.Id ?? defaultType?.Id;
                    }
                    else if (ev.Title.Contains("Gala", StringComparison.OrdinalIgnoreCase) || ev.Title.Contains("Healthcare", StringComparison.OrdinalIgnoreCase))
                    {
                        ev.EventTypeId = galaType?.Id ?? defaultType?.Id;
                    }
                    else if (ev.Title.Contains("Expo", StringComparison.OrdinalIgnoreCase) || ev.Title.Contains("Design", StringComparison.OrdinalIgnoreCase))
                    {
                        ev.EventTypeId = expoType?.Id ?? defaultType?.Id;
                    }
                    else
                    {
                        ev.EventTypeId = defaultType?.Id;
                    }
                }
                context.SaveChanges();
            }

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
                    IsAvailable = true,
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
                    IsAvailable = true,
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
                    IsAvailable = true,
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
                    EventTypeId = "et-001", // Conference
                    ImageUrl = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?auto=format&fit=crop&w=800&q=80",
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
                    EventTypeId = "et-005", // Gala Dinner
                    ImageUrl = "https://images.unsplash.com/photo-1511795409834-ef04bbd61622?auto=format&fit=crop&w=800&q=80",
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
                    EventTypeId = "et-006", // Exhibition
                    ImageUrl = "https://images.unsplash.com/photo-1475721027785-f74eccf877e2?auto=format&fit=crop&w=800&q=80",
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
