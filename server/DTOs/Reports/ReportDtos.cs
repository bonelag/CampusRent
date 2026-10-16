using System.ComponentModel.DataAnnotations;
using CampusRent.Server.DTOs.Auth;
using CampusRent.Server.Models;

namespace CampusRent.Server.DTOs.Reports;

public class CreateReportDto
{
    public Guid? RoomId { get; set; }
    public Guid? ReviewId { get; set; }

    [Required, MaxLength(255)]
    public string Reason { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class ResolveReportDto
{
    [Required]
    public ReportStatus Status { get; set; } // RESOLVED or DISMISSED

    public bool DeleteTarget { get; set; } = false;
}

public class ReportItemDto
{
    public Guid Id { get; set; }
    public Guid ReporterId { get; set; }
    public UserProfileDto Reporter { get; set; } = null!;
    public Guid? RoomId { get; set; }
    public string? RoomTitle { get; set; }
    public Guid? ReviewId { get; set; }
    public string? ReviewComment { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ReportStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
