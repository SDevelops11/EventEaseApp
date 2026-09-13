using EventEaseApp.Data;
using EventEaseApp.Models;
using EventEaseApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEaseApp.Controllers
{
    public class VenuesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBlobService _blobService;

        public VenuesController(ApplicationDbContext context, IBlobService blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        // GET: Venues with Multi-Criteria Search
        public async Task<IActionResult> Index(string? searchTerm, string? status, int? minCapacity, decimal? maxRate)
        {
            var query = _context.Venues.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(v => v.Name.ToLower().Contains(term) || 
                                         v.Location.ToLower().Contains(term) || 
                                         (v.Description != null && v.Description.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(v => v.Status == status);
            }

            if (minCapacity.HasValue && minCapacity.Value > 0)
            {
                query = query.Where(v => v.Capacity >= minCapacity.Value);
            }

            if (maxRate.HasValue && maxRate.Value > 0)
            {
                query = query.Where(v => v.HourlyRate <= maxRate.Value);
            }

            ViewData["CurrentSearch"] = searchTerm;
            ViewData["CurrentStatus"] = status ?? "All";
            ViewData["CurrentMinCapacity"] = minCapacity;
            ViewData["CurrentMaxRate"] = maxRate;

            return View(await query.OrderBy(v => v.Name).ToListAsync());
        }

        // GET: Venues/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Venues/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Venue venue, IFormFile? imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    venue.ImageUrl = await _blobService.UploadFileAsync(imageFile, "venues");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("imageFile", ex.Message);
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(venue);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(venue);
        }

        // GET: Venues/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var venue = await _context.Venues.FindAsync(id);
            if (venue == null) return NotFound();

            return View(venue);
        }

        // POST: Venues/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Venue venue, IFormFile? imageFile)
        {
            if (id != venue.Id) return NotFound();

            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    venue.ImageUrl = await _blobService.UploadFileAsync(imageFile, "venues");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("imageFile", ex.Message);
                }
            }
            else if (string.IsNullOrWhiteSpace(venue.ImageUrl))
            {
                // Retain previous image if field left blank
                var existing = await _context.Venues.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
                if (existing != null)
                {
                    venue.ImageUrl = existing.ImageUrl;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(venue);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Venues.Any(v => v.Id == venue.Id))
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
            return View(venue);
        }

        // POST: Venues/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null) return NotFound();

            try
            {
                _context.Venues.Remove(venue);
                await _context.SaveChangesAsync();

                // Clean up blob if stored in Azure
                if (!string.IsNullOrEmpty(venue.ImageUrl))
                {
                    _ = _blobService.DeleteFileAsync(venue.ImageUrl);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                // Prevent deletion if venue has associated bookings
                TempData["ErrorMessage"] = "Cannot delete this venue because it has active or historical bookings.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}