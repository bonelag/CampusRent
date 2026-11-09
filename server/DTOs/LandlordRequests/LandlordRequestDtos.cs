using System.ComponentModel.DataAnnotations;
using CampusRent.Server.Models;

namespace CampusRent.Server.DTOs.LandlordRequests;

public class CreateLandlordRequestDto
{
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề yêu cầu.")]
    [MaxLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập lý do nâng cấp tài khoản.")]
    [MaxLength(2000, ErrorMessage = "Lý do không được vượt quá 2000 ký tự.")]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(10)]
    public List<string> ImageUrls { get; set; } = new();

    [MaxLength(2048)]
    public string? PdfUrl { get; set; }

    [MaxLength(255)]
    public string? PdfFileName { get; set; }
}

public class RejectLandlordRequestDto
{
    [MaxLength(1000)]
    public string? AdminNote { get; set; }
}

public class LandlordRequestResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? UserPhoneNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = new();
    public string? PdfUrl { get; set; }
    public string? PdfFileName { get; set; }
    public LandlordRequestStatus Status { get; set; }
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
