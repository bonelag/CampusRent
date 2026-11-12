using System.ComponentModel.DataAnnotations;
using CampusRent.Server.Models;

namespace CampusRent.Server.DTOs.Admin;

public sealed record AdminResetPasswordDto(
    [Required, MinLength(8), MaxLength(72)] string Password);

public sealed record AdminRoomStatusDto(RoomStatus Status, bool Renew = false);

public sealed record AdminBookingStatusDto(
    [Required, MaxLength(20)] string Status);

public sealed record AdminReviewUpdateDto(
    bool IsFlagged,
    [MaxLength(255)] string? FlagReason);
