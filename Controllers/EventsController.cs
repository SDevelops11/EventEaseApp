using EventEaseApp.Data;
using EventEaseApp.Models;
using EventEaseApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventEaseApp.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBlobService _blobService;

        public EventsController(ApplicationDbContext context, IBlobService blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        // GET: Events with Multi-Criteria Search
        public async Task<IActionResult> Index(string? searchTerm, string? status, string? venueId, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Events.Include(e => e.Venue).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(e => e.Title.ToLower().Contains(term) ||
                                         e.ClientName.ToLower().Contains(term) ||
                                         e.ClientEmail.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(e => e.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(venueId) && venueId != "All")
            {
                query = query.Where(e => e.VenueId == venueId);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(e => e.StartDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(e => e.EndDate <= toDate.Value);
            }

            ViewData["CurrentSearch"] = searchTerm;
            ViewData["CurrentStatus"] = status ?? "All";
            ViewData["CurrentVenueId"] = venueId ?? "All";
            ViewData["CurrentFromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["CurrentToDate"] = toDate?.ToString("yyyy-MM-dd");
            ViewData["VenuesList"] = new SelectList(await _context.Venues.OrderBy(v => v.Name).ToListAsync(), "Id", "Name", venueId);

            var events = await query.OrderByDescending(e => e.StartDate).ToListAsync();
            return View(events);
        }

        // GET: Events/Create
        public async Task<IActionResult> Create()
        {
            ViewData["VenueId"] = new SelectList(await _context.Venues.Where(v => v.Status == "Active").ToListAsync(), "Id", "Name");
            return View();
        }

        // POST: Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event @event, IFormFile? imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    @event.ImageUrl = await _blobService.UploadFileAsync(imageFile, "events");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("imageFile", ex.Message);
                }
            }

            if (ModelState.IsValid)
            {
                @event.Status = string.IsNullOrEmpty(@event.VenueId) ? "Unassigned" : "Preferred Venue Selected";
                _context.Add(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["VenueId"] = new SelectList(await _context.Venues.ToListAsync(), "Id", "Name", @event.VenueId);
            return View(@event);
        }

        // GET: Events/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var @event = await _context.Events.FindAsync(id);
            if (@event == null) return NotFound();

            ViewData["VenueId"] = new SelectList(await _context.Venues.ToListAsync(), "Id", "Name", @event.VenueId);
            return View(@event);
        }

        // POST: Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Event @event, IFormFile? imageFile)
        {
            if (id != @event.Id) return NotFound();

            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    @event.ImageUrl = await _blobService.UploadFileAsync(imageFile, "events");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("imageFile", ex.Message);
                }
            }
            else if (string.IsNullOrWhiteSpace(@event.ImageUrl))
            {
                // Retain previous image if field left blank
                var existing = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
                if (existing != null)
                {
                    @event.ImageUrl = existing.ImageUrl;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if event has an active booking
                    bool isBooked = await _context.Bookings.AnyAsync(b => b.EventId == @event.Id);
                    if (isBooked)
                    {
                        @event.Status = "Booked";
                        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.EventId == @event.Id);
                        if (booking != null && !string.IsNullOrEmpty(@event.VenueId) && booking.VenueId != @event.VenueId)
                        {
                            booking.VenueId = @event.VenueId;
                            _context.Update(booking);
                        }
                    }
                    else if (@event.Status != "Completed" && @event.Status != "Cancelled")
                    {
                        @event.Status = string.IsNullOrEmpty(@event.VenueId) ? "Unassigned" : "Preferred Venue Selected";
                    }

                    _context.Update(@event);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Events.Any(e => e.Id == @event.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["VenueId"] = new SelectList(await _context.Venues.ToListAsync(), "Id", "Name", @event.VenueId);
            return View(@event);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event == null) return NotFound();

            bool hasBooking = await _context.Bookings.AnyAsync(b => b.EventId == id);
            if (hasBooking)
            {
                TempData["ErrorMessage"] = "Cannot delete this event because it is currently linked to an active booking.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();

                // Clean up blob if stored in Azure
                if (!string.IsNullOrEmpty(@event.ImageUrl))
                {
                    _ = _blobService.DeleteFileAsync(@event.ImageUrl);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Cannot delete this event due to a database integrity constraint.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}