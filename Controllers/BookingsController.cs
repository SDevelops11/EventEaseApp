using EventEaseApp.Data;
using EventEaseApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventEaseApp.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Bookings
        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Event)
                .ToListAsync();
            return View(bookings);
        }

        // GET: Bookings/Create
        public async Task<IActionResult> Create()
        {
            // ERD Rule: Filter out Events that are ALREADY booked (1:1 Constraint)
            var unbookedEvents = await _context.Events
                .Where(e => !_context.Bookings.Any(b => b.EventId == e.Id))
                .ToListAsync();

            var venues = await _context.Venues.Where(v => v.Status == "Active").ToListAsync();
            if (!venues.Any())
            {
                venues = await _context.Venues.ToListAsync();
            }

            ViewData["EventId"] = new SelectList(unbookedEvents, "Id", "Title");
            ViewData["VenueId"] = new SelectList(venues, "Id", "Name");

            return View();
        }

        // POST: Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking booking)
        {
            ModelState.Remove("BookingCode");
            ModelState.Remove("TotalCost");
            ModelState.Remove("Venue");
            ModelState.Remove("Event");

            // Prevent double-booking constraint check
            bool alreadyBooked = await _context.Bookings.AnyAsync(b => b.EventId == booking.EventId);
            if (alreadyBooked)
            {
                ModelState.AddModelError("EventId", "This event has already been booked!");
            }

            if (ModelState.IsValid)
            {
                // 1. Generate unique booking code
                booking.BookingCode = "BK-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

                // 2. Calculate Total Cost based on Venue Hourly Rate
                var venue = await _context.Venues.FindAsync(booking.VenueId);
                if (venue != null)
                {
                    var hours = (booking.EndDate - booking.StartDate).TotalHours;
                    booking.TotalCost = venue.HourlyRate * (decimal)(hours > 0 ? hours : 1);
                }

                // 3. Update associated Event Status to 'Booked'
                var selectedEvent = await _context.Events.FindAsync(booking.EventId);
                if (selectedEvent != null)
                {
                    selectedEvent.Status = "Booked";
                    selectedEvent.VenueId = booking.VenueId; // Set final assigned venue
                    _context.Update(selectedEvent);
                }

                _context.Add(booking);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var unbookedEvents = await _context.Events.Where(e => !_context.Bookings.Any(b => b.EventId == e.Id)).ToListAsync();
            ViewData["EventId"] = new SelectList(unbookedEvents, "Id", "Title", booking.EventId);
            ViewData["VenueId"] = new SelectList(await _context.Venues.ToListAsync(), "Id", "Name", booking.VenueId);
            return View(booking);
        }
    }
}