using System.ComponentModel.DataAnnotations;
namespace CampusRent.Server.Models;
public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;
    public Guid StudentId { get; set; }
    public User Student { get; set; } = null!;
    public DateTime VisitAt { get; set; }
    [MaxLength(1000)] public string Note { get; set; } = "";
    [MaxLength(20)] public string Status { get; set; } = "PENDING";
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ReminderSentAt { get; set; }
    public DateTime? CustomerNotClosedAt { get; set; }
    public DateTime? AutoClosedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
