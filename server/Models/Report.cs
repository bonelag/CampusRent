using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusRent.Server.Models;

[Table("reports")]
public class Report
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("reporter_id")]
    public Guid ReporterId { get; set; }

    [ForeignKey(nameof(ReporterId))]
    public User Reporter { get; set; } = null!;

    [Column("room_id")]
    public Guid? RoomId { get; set; }

    [ForeignKey(nameof(RoomId))]
    public Room? Room { get; set; }

    [Column("review_id")]
    public Guid? ReviewId { get; set; }

    [ForeignKey(nameof(ReviewId))]
    public Review? Review { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("reason")]
    public string Reason { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    public ReportStatus Status { get; set; } = ReportStatus.PENDING;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
