using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusRent.Server.Models;

[Table("landlord_requests")]
public class LandlordRequest
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("reason")]
    public string Reason { get; set; } = string.Empty;

    [Column("image_urls")]
    public List<string> ImageUrls { get; set; } = new();

    [Column("pdf_url")]
    public string? PdfUrl { get; set; }

    [MaxLength(255)]
    [Column("pdf_file_name")]
    public string? PdfFileName { get; set; }

    [Column("status")]
    public LandlordRequestStatus Status { get; set; } = LandlordRequestStatus.PENDING;

    [Column("admin_note")]
    public string? AdminNote { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
