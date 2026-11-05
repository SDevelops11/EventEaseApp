using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventEaseApp.Models
{
    [Table("dbo_EventTypes")]
    public class EventType
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required(ErrorMessage = "Event Type name is required.")]
        [StringLength(100)]
        [Display(Name = "Event Type")]
        public string Name { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Icon { get; set; } = "bi-calendar-event";

        // Navigation property (1:N relationship with Event)
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
