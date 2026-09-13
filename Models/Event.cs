using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventEaseApp.Models
{
    [Table("dbo_Events")]
    public class Event
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(250)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string ClientName { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [EmailAddress]
        public string ClientEmail { get; set; } = string.Empty;

        public int ExpectedAttendance { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        // Nullable FK (1:N Preferred Venue / Unassigned)
        public string? VenueId { get; set; }

        [ForeignKey("VenueId")]
        public Venue? Venue { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Unassigned"; // Unassigned | Booked | Completed | Cancelled

        public string? ImageUrl { get; set; }

        // Navigation Property (1:1 Relationship with Booking)
        public Booking? Booking { get; set; }
    }
}