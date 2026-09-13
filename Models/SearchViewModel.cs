namespace EventEaseApp.Models
{
    public class SearchViewModel
    {
        public string? Query { get; set; }
        public string Category { get; set; } = "All"; // All | Venues | Events | Bookings
        public List<Venue> Venues { get; set; } = new();
        public List<Event> Events { get; set; } = new();
        public List<Booking> Bookings { get; set; } = new();

        public int TotalCount => Venues.Count + Events.Count + Bookings.Count;
    }
}
