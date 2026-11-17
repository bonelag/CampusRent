using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusRent.Server.Models;

[Table("smtp_settings")]
public class SmtpSetting
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("id")]
    public int Id { get; set; } = 1;

    [Column("enabled")]
    public bool Enabled { get; set; } = false;

    [Required]
    [MaxLength(255)]
    [Column("host")]
    public string Host { get; set; } = "smtp.gmail.com";

    [Range(1, 65535)]
    [Column("port")]
    public int Port { get; set; } = 587;

    [Column("use_ssl")]
    public bool UseSsl { get; set; } = true;

    [MaxLength(255)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [MaxLength(512)]
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("from_email")]
    public string FromEmail { get; set; } = string.Empty;

    [MaxLength(150)]
    [Column("from_name")]
    public string FromName { get; set; } = "CampusRent";

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
