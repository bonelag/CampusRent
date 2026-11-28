using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Auth;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.Models;
using CampusRent.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CampusRent.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    CampusRentDbContext db,
    ITokenService tokens,
    IEmailService emailService,
    IHostEnvironment environment) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterRequestDto dto)
    {
        if (dto.Role != Role.STUDENT)
        {
            return BadRequest(ApiResponse<object>.Fail("Tài khoản mới là người tìm trọ. Gửi yêu cầu nâng cấp để được duyệt quyền chủ trọ."));
        }

        if (Encoding.UTF8.GetByteCount(dto.Password) > 72)
        {
            return BadRequest(ApiResponse<object>.Fail("Mật khẩu vượt quá 72 byte. Vui lòng chọn mật khẩu ngắn hơn."));
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            return Conflict(ApiResponse<object>.Fail("Email đã được sử dụng."));
        }

        var user = new User
        {
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FullName = dto.FullName.Trim(),
            PhoneNumber = dto.PhoneNumber,
            Role = dto.Role,
            IsEduVerified = false
        };

        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(ApiResponse<object>.Fail("Email đã được sử dụng."));
        }

        return Ok(ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token = tokens.GenerateToken(user),
            User = Mapping.Profile(user)
        }));
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginRequestDto dto)
    {
        if (Encoding.UTF8.GetByteCount(dto.Password) > 72)
        {
            return Unauthorized(ApiResponse<object>.Fail("Email hoặc mật khẩu không chính xác."));
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
        {
            return Unauthorized(ApiResponse<object>.Fail("Email hoặc mật khẩu không chính xác."));
        }

        return Ok(ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token = tokens.GenerateToken(user),
            User = Mapping.Profile(user)
        }));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await db.Users.FindAsync(User.UserId());
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(ApiResponse<UserProfileDto>.Ok(Mapping.Profile(user)));
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> Update(ProfileUpdate dto)
    {
        var user = await db.Users.FindAsync(User.UserId());
        if (user == null)
        {
            return Unauthorized();
        }

        user.FullName = dto.FullName.Trim();
        user.PhoneNumber = dto.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(ApiResponse<UserProfileDto>.Ok(Mapping.Profile(user)));
    }

    [Authorize]
    [HttpPut("password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var user = await db.Users.FindAsync(User.UserId());
        if (user == null)
        {
            return Unauthorized();
        }

        if (Encoding.UTF8.GetByteCount(dto.CurrentPassword) > 72 ||
            !BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.Password))
        {
            return BadRequest(ApiResponse<object>.Fail("Mật khẩu hiện tại không chính xác."));
        }

        if (Encoding.UTF8.GetByteCount(dto.NewPassword) > 72)
        {
            return BadRequest(ApiResponse<object>.Fail("Mật khẩu mới không được vượt quá 72 byte."));
        }

        if (dto.NewPassword == dto.CurrentPassword)
        {
            return BadRequest(ApiResponse<object>.Fail("Mật khẩu mới cần khác mật khẩu hiện tại."));
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token = tokens.GenerateToken(user),
            User = Mapping.Profile(user)
        }));
    }

    [Authorize]
    [HttpPost("send-verification")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> SendVerificationCode(CancellationToken ct)
    {
        var user = await db.Users.FindAsync([User.UserId()], ct);
        if (user == null)
        {
            return Unauthorized();
        }

        if (user.IsEmailVerified)
        {
            return BadRequest(ApiResponse<object>.Fail("Email tài khoản đã được xác minh."));
        }

        var now = DateTime.UtcNow;
        if (user.EmailVerificationSentAt.HasValue && now - user.EmailVerificationSentAt.Value < TimeSpan.FromSeconds(60))
        {
            var waitSeconds = Math.Max(1, (int)Math.Ceiling(60 - (now - user.EmailVerificationSentAt.Value).TotalSeconds));
            return StatusCode(
                StatusCodes.Status429TooManyRequests,
                ApiResponse<object>.Fail($"Vui lòng đợi {waitSeconds} giây trước khi gửi lại mã xác minh."));
        }

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        user.EmailVerificationCodeHash = HashVerificationCode(user.Id, code);
        user.EmailVerificationExpiresAt = now.AddMinutes(15);
        user.EmailVerificationSentAt = now;
        user.EmailVerificationAttempts = 0;

        var delivery = await emailService.SendVerificationCodeAsync(user.Email, user.FullName, code, ct);
        if (!delivery.Sent && !delivery.IsSimulated)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.Fail(delivery.Error ?? "Không thể gửi email xác minh. Vui lòng thử lại sau."));
        }

        await db.SaveChangesAsync(ct);

        var exposePreview = delivery.IsSimulated && (environment.IsDevelopment() || environment.IsEnvironment("Testing"));
        var message = delivery.Sent
            ? $"Đã gửi mã xác minh 6 chữ số tới {user.Email}."
            : "SMTP chưa bật (chế độ thử nghiệm): hãy dùng mã xem trước để xác minh.";

        return Ok(ApiResponse<object>.Ok(new
        {
            sent = delivery.Sent,
            simulated = delivery.IsSimulated,
            expiresAt = user.EmailVerificationExpiresAt,
            previewCode = exposePreview ? code : null
        }, message));
    }

    [Authorize]
    [HttpPost("verify-email")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailDto dto, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([User.UserId()], ct);
        if (user == null)
        {
            return Unauthorized();
        }

        if (user.IsEmailVerified)
        {
            return Ok(ApiResponse<UserProfileDto>.Ok(Mapping.Profile(user), "Email đã được xác minh trước đó."));
        }

        var now = DateTime.UtcNow;
        if (string.IsNullOrEmpty(user.EmailVerificationCodeHash) ||
            !user.EmailVerificationExpiresAt.HasValue ||
            user.EmailVerificationExpiresAt.Value <= now)
        {
            return BadRequest(ApiResponse<object>.Fail("Mã xác minh đã hết hạn hoặc chưa được tạo. Vui lòng gửi lại mã mới."));
        }

        var submittedHash = HashVerificationCode(user.Id, dto.Code.Trim());
        var expectedBytes = Encoding.UTF8.GetBytes(user.EmailVerificationCodeHash);
        var actualBytes = Encoding.UTF8.GetBytes(submittedHash);

        if (!CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes))
        {
            user.EmailVerificationAttempts++;
            if (user.EmailVerificationAttempts >= 5)
            {
                user.EmailVerificationCodeHash = null;
                user.EmailVerificationExpiresAt = null;
                await db.SaveChangesAsync(ct);
                return BadRequest(ApiResponse<object>.Fail("Bạn đã nhập sai quá 5 lần. Vui lòng yêu cầu gửi lại mã mới."));
            }

            await db.SaveChangesAsync(ct);
            return BadRequest(ApiResponse<object>.Fail("Mã xác minh không chính xác."));
        }

        user.IsEmailVerified = true;
        if (Models.User.IsEduDomain(user.Email))
        {
            user.IsEduVerified = true;
        }

        user.EmailVerificationCodeHash = null;
        user.EmailVerificationExpiresAt = null;
        user.EmailVerificationAttempts = 0;
        user.UpdatedAt = now;
        await db.SaveChangesAsync(ct);

        var message = user.IsEduVerified
            ? "Đã xác minh email thành công và kích hoạt huy hiệu Sinh viên (.edu)!"
            : "Đã xác minh địa chỉ email thành công!";

        return Ok(ApiResponse<UserProfileDto>.Ok(Mapping.Profile(user), message));
    }

    private static string HashVerificationCode(Guid userId, string code)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{userId}:{code}")));
    }
}

public record ProfileUpdate(
    [Required, MaxLength(150)] string FullName,
    [MaxLength(20)] string? PhoneNumber);

public record ChangePasswordDto(
    [Required, MaxLength(72)] string CurrentPassword,
    [Required, MinLength(8), MaxLength(72)] string NewPassword);
