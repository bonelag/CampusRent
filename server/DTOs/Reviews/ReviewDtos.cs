using System.ComponentModel.DataAnnotations;
using CampusRent.Server.DTOs.Auth;

namespace CampusRent.Server.DTOs.Reviews;

public class CreateReviewDto
{
    [Range(1, 5)]
    public int Rating { get; set; }

    [Required, MinLength(2), MaxLength(3000)]
    public string Comment { get; set; } = string.Empty;
}

public class ReviewItemDto
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public bool IsFlagged { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserProfileDto User { get; set; } = null!;
}
