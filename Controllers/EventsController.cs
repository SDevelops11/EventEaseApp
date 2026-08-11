using EventEaseApp.Data;
using EventEaseApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventEaseApp.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Events
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events.Include(e => e.Venue).ToListAsync();
            return View(events);
        }

        // GET: Events/Create
        public async Task<IActionResult> Create()
        {
            // Load venues for dropdown selection (Optional / Nullable)
            ViewData["VenueId"] = new SelectList(await _context.Venues.Where(v => v.Status == "Active").ToListAsync(), "Id", "Name");
            return View();
        }

        // POST: Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event @event)
        {
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
    }
}