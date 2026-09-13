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

        // GET: Bookings with Multi-Criteria Search & Consolidated Analytics
        public async Task<IActionResult> Index(string? searchTerm, string? status, string? venueId, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Event)
                .AsQueryable();

            // Overall metrics for KPI summary cards
            var allBookings = await _context.Bookings.ToListAsync();
            ViewData["TotalBookingsCount"] = allBookings.Count;
            ViewData["TotalRevenue"] = allBookings.Where(b => b.Status != "Cancelled").Sum(b => b.TotalCost);
            ViewData["ConfirmedCount"] = allBookings.Count(b => b.Status == "Confirmed");
            ViewData["PendingCount"] = allBookings.Count(b => b.Status == "Pending");

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(b => b.BookingCode.ToLower().Contains(term) ||
                                         b.SpecialistName.ToLower().Contains(term) ||
                                         (b.Event != null && b.Event.Title.ToLower().Contains(term)) ||
                                         (b.Venue != null && b.Venue.Name.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(b => b.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(venueId) && venueId != "All")
            {
                query = query.Where(e => e.VenueId == venueId);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(b => b.StartDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(b => b.EndDate <= toDate.Value);
            }

            ViewData["CurrentSearch"] = searchTerm;
            ViewData["CurrentStatus"] = status ?? "All";
            ViewData["CurrentVenueId"] = venueId ?? "All";
            ViewData["CurrentFromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["CurrentToDate"] = toDate?.ToString("yyyy-MM-dd");
            ViewData["VenuesList"] = new SelectList(await _context.Venues.OrderBy(v => v.Name).ToListAsync(), "Id", "Name", venueId);

            var bookings = await query.OrderByDescending(b => b.StartDate).ToListAsync();
            return View(bookings);
        }

        // GET: Bookings/Create
        public async Task<IActionResult> Create()
        {
            // Fetch events that haven't been booked yet
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

            // Check start/end date validity
            if (booking.EndDate <= booking.StartDate)
            {
                ModelState.AddModelError("EndDate", "End Date & Time must be strictly after the Start Date & Time.");
            }

            // Check if event is already booked
            bool alreadyBooked = await _context.Bookings.AnyAsync(b => b.EventId == booking.EventId);
            if (alreadyBooked)
            {
                ModelState.AddModelError("EventId", "This event has already been booked!");
            }

            // Check for venue double-booking (overlapping date ranges)
            if (!string.IsNullOrEmpty(booking.VenueId))
            {
                bool venueOverlap = await _context.Bookings.AnyAsync(b => 
                    b.VenueId == booking.VenueId && 
                    b.Status != "Cancelled" && 
                    booking.StartDate < b.EndDate && 
                    booking.EndDate > b.StartDate);
                if (venueOverlap)
                {
                    ModelState.AddModelError("VenueId", "This venue is already booked for the selected schedule. Please select another venue or adjust the time.");
                }
            }

            if (ModelState.IsValid)
            {
                // Generate unique booking code
                booking.BookingCode = "BK-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

                // Calculate total cost based on duration and hourly rate
                var venue = await _context.Venues.FindAsync(booking.VenueId);
                if (venue != null)
                {
                    var hours = (booking.EndDate - booking.StartDate).TotalHours;
                    booking.TotalCost = venue.HourlyRate * (decimal)(hours > 0 ? hours : 1);
                }

                // Update event status and venue assignment
                var selectedEvent = await _context.Events.FindAsync(booking.EventId);
                if (selectedEvent != null)
                {
                    selectedEvent.Status = "Booked";
                    selectedEvent.VenueId = booking.VenueId;
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

        // GET: Bookings/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            var availableEvents = await _context.Events
                .Where(e => e.Id == booking.EventId || !_context.Bookings.Any(b => b.EventId == e.Id))
                .ToListAsync();

            ViewData["EventId"] = new SelectList(availableEvents, "Id", "Title", booking.EventId);
            ViewData["VenueId"] = new SelectList(await _context.Venues.ToListAsync(), "Id", "Name", booking.VenueId);
            return View(booking);
        }

        // POST: Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Booking booking)
        {
            if (id != booking.Id) return NotFound();

            ModelState.Remove("BookingCode");
            ModelState.Remove("TotalCost");
            ModelState.Remove("Venue");
            ModelState.Remove("Event");

            // Check start/end date validity
            if (booking.EndDate <= booking.StartDate)
            {
                ModelState.AddModelError("EndDate", "End Date & Time must be strictly after the Start Date & Time.");
            }

            bool alreadyBooked = await _context.Bookings.AnyAsync(b => b.EventId == booking.EventId && b.Id != booking.Id);
            if (alreadyBooked)
            {
                ModelState.AddModelError("EventId", "This event has already been booked by another booking!");
            }

            // Check for venue double-booking (overlapping date ranges)
            if (!string.IsNullOrEmpty(booking.VenueId))
            {
                bool venueOverlap = await _context.Bookings.AnyAsync(b => 
                    b.VenueId == booking.VenueId && 
                    b.Id != booking.Id && 
                    b.Status != "Cancelled" && 
                    booking.StartDate < b.EndDate && 
                    booking.EndDate > b.StartDate);
                if (venueOverlap)
                {
                    ModelState.AddModelError("VenueId", "This venue is already booked for the selected schedule. Please select another venue or adjust the time.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingBooking = await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
                    if (existingBooking != null)
                    {
                        booking.BookingCode = existingBooking.BookingCode;
                    }

                    // Recalculate total cost based on venue rate and duration
                    var venue = await _context.Venues.FindAsync(booking.VenueId);
                    if (venue != null)
                    {
                        var hours = (booking.EndDate - booking.StartDate).TotalHours;
                        booking.TotalCost = venue.HourlyRate * (decimal)(hours > 0 ? hours : 1);
                    }

                    // If Event changed, revert previous event status
                    if (existingBooking != null && existingBooking.EventId != booking.EventId)
                    {
                        var oldEvent = await _context.Events.FindAsync(existingBooking.EventId);
                        if (oldEvent != null)
                        {
                            oldEvent.Status = string.IsNullOrEmpty(oldEvent.VenueId) ? "Unassigned" : "Preferred Venue Selected";
                            _context.Update(oldEvent);
                        }
                    }

                    // Update new event status and venue
                    var newEvent = await _context.Events.FindAsync(booking.EventId);
                    if (newEvent != null)
                    {
                        newEvent.Status = "Booked";
                        newEvent.VenueId = booking.VenueId;
                        _context.Update(newEvent);
                    }

                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Bookings.Any(b => b.Id == booking.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            var availableEvents = await _context.Events
                .Where(e => e.Id == booking.EventId || !_context.Bookings.Any(b => b.EventId == e.Id))
                .ToListAsync();
            ViewData["EventId"] = new SelectList(availableEvents, "Id", "Title", booking.EventId);
            ViewData["VenueId"] = new SelectList(await _context.Venues.ToListAsync(), "Id", "Name", booking.VenueId);
            return View(booking);
        }

        // POST: Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            // Reset linked event status back to Unassigned or Preferred Venue Selected
            var linkedEvent = await _context.Events.FindAsync(booking.EventId);
            if (linkedEvent != null)
            {
                linkedEvent.Status = string.IsNullOrEmpty(linkedEvent.VenueId) ? "Unassigned" : "Preferred Venue Selected";
                _context.Update(linkedEvent);
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}