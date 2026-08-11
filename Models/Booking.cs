using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EventEaseApp.Models
{
    [Table("dbo_Bookings")]
    public class Booking
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [ValidateNever]
        public string BookingCode { get; set; } = string.Empty;

        // Foreign Key to Venue
        [Required(ErrorMessage = "Please select a venue.")]
        public string VenueId { get; set; } = string.Empty;

        [ForeignKey("VenueId")]
        [ValidateNever]
        public Venue? Venue { get; set; }

        // Foreign Key to Event (Unique 1:1 constraint)
        [Required(ErrorMessage = "Please select an event.")]
        public string EventId { get; set; } = string.Empty;

        [ForeignKey("EventId")]
        [ValidateNever]
        public Event? Event { get; set; }

        [Required(ErrorMessage = "Specialist name is required.")]
        [StringLength(150)]
        public string SpecialistName { get; set; } = string.Empty;

        public DateTime StartDate { get; set; } = DateTime.Now;

        public DateTime EndDate { get; set; } = DateTime.Now.AddHours(2);

        [ValidateNever]
        public decimal TotalCost { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Confirmed"; // Confirmed | Pending | Cancelled
    }
}