using System.ComponentModel.DataAnnotations;
using CampusRent.Server.Models;

namespace CampusRent.Server.DTOs.Admin;

public sealed record AdminResetPasswordDto(
    [Required, MinLength(8), MaxLength(72)] string Password);

public sealed record AdminUserVerificationDto(
    bool IsEmailVerified,
    bool IsEduVerified);

public sealed record AdminRoomStatusDto(RoomStatus Status, bool Renew = false);

public sealed record AdminBookingStatusDto(
    [Required, MaxLength(20)] string Status);

public sealed record AdminReviewUpdateDto(
    bool IsFlagged,
    [MaxLength(255)] string? FlagReason);

public sealed record SmtpSettingsUpdateDto(
    bool Enabled,
    [Required, MaxLength(255)] string Host,
    [Range(1, 65535)] int Port,
    bool UseSsl,
    [MaxLength(255)] string Username,
    [MaxLength(255)] string? Password,
    [MaxLength(255)] string FromEmail,
    [MaxLength(150)] string FromName);

public sealed record AdminTestEmailDto(
    [EmailAddress, MaxLength(255)] string? ToEmail);
