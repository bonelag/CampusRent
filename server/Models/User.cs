using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusRent.Server.Models;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("phone_number")]
    public string? PhoneNumber { get; set; }

    [Column("avatar")]
    public string? Avatar { get; set; }

    [Column("role")]
    public Role Role { get; set; } = Role.STUDENT;

    [Column("is_edu_verified")]
    public bool IsEduVerified { get; set; } = false;

    [Column("is_email_verified")]
    public bool IsEmailVerified { get; set; } = false;

    [MaxLength(128)]
    [Column("email_verification_code_hash")]
    public string? EmailVerificationCodeHash { get; set; }

    [Column("email_verification_expires_at")]
    public DateTime? EmailVerificationExpiresAt { get; set; }

    [Column("email_verification_sent_at")]
    public DateTime? EmailVerificationSentAt { get; set; }

    [Column("email_verification_attempts")]
    public int EmailVerificationAttempts { get; set; } = 0;

    public static bool IsEduDomain(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var at = email.Trim().LastIndexOf('@');
        if (at < 0 || at == email.Trim().Length - 1) return false;
        var domain = email.Trim()[(at + 1)..];
        return domain.EndsWith(".edu", StringComparison.OrdinalIgnoreCase)
            || domain.EndsWith(".edu.vn", StringComparison.OrdinalIgnoreCase);
    }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
    public ICollection<LandlordRequest> LandlordRequests { get; set; } = new List<LandlordRequest>();
}
