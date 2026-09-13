using EventEaseApp.Data;
using EventEaseApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEaseApp.Controllers
{
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Search
        public async Task<IActionResult> Index(string? query, string category = "All")
        {
            var viewModel = new SearchViewModel
            {
                Query = query?.Trim(),
                Category = string.IsNullOrWhiteSpace(category) ? "All" : category
            };

            if (string.IsNullOrWhiteSpace(viewModel.Query))
            {
                return View(viewModel);
            }

            var q = viewModel.Query.ToLower();

            // Search Venues
            if (viewModel.Category == "All" || viewModel.Category == "Venues")
            {
                viewModel.Venues = await _context.Venues
                    .Where(v => v.Name.ToLower().Contains(q) ||
                                v.Location.ToLower().Contains(q) ||
                                (v.Description != null && v.Description.ToLower().Contains(q)) ||
                                v.Status.ToLower().Contains(q))
                    .OrderBy(v => v.Name)
                    .ToListAsync();
            }

            // Search Events
            if (viewModel.Category == "All" || viewModel.Category == "Events")
            {
                viewModel.Events = await _context.Events
                    .Include(e => e.Venue)
                    .Where(e => e.Title.ToLower().Contains(q) ||
                                e.ClientName.ToLower().Contains(q) ||
                                e.ClientEmail.ToLower().Contains(q) ||
                                (e.Venue != null && e.Venue.Name.ToLower().Contains(q)) ||
                                e.Status.ToLower().Contains(q))
                    .OrderByDescending(e => e.StartDate)
                    .ToListAsync();
            }

            // Search Bookings
            if (viewModel.Category == "All" || viewModel.Category == "Bookings")
            {
                viewModel.Bookings = await _context.Bookings
                    .Include(b => b.Venue)
                    .Include(b => b.Event)
                    .Where(b => b.BookingCode.ToLower().Contains(q) ||
                                b.SpecialistName.ToLower().Contains(q) ||
                                b.Status.ToLower().Contains(q) ||
                                (b.Venue != null && b.Venue.Name.ToLower().Contains(q)) ||
                                (b.Event != null && b.Event.Title.ToLower().Contains(q)))
                    .OrderByDescending(b => b.StartDate)
                    .ToListAsync();
            }

            return View(viewModel);
        }
    }
}
