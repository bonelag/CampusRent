using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusRent.Server.Models;

[Table("reviews")]
public class Review
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("room_id")]
    public Guid RoomId { get; set; }

    [ForeignKey(nameof(RoomId))]
    public Room Room { get; set; } = null!;

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Range(1, 5)]
    [Column("rating")]
    public int Rating { get; set; }

    [Required]
    [Column("comment")]
    public string Comment { get; set; } = string.Empty;

    [Column("is_flagged")]
    public bool IsFlagged { get; set; } = false;

    [Column("flag_reason")]
    public string? FlagReason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Report> Reports { get; set; } = new List<Report>();
}
