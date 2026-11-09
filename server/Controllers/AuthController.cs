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
[ApiController, Route("api/auth")]
public class AuthController(CampusRentDbContext db, ITokenService tokens) : ControllerBase
{
    [HttpPost("register"), EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterRequestDto dto)
    {
        if (dto.Role != Role.STUDENT) return BadRequest(ApiResponse<object>.Fail("Tài khoản mới là người tìm trọ. Gửi yêu cầu nâng cấp để được duyệt quyền chủ trọ."));
        if (System.Text.Encoding.UTF8.GetByteCount(dto.Password) > 72) return BadRequest(ApiResponse<object>.Fail("Mật khẩu vượt quá 72 byte. Vui lòng chọn mật khẩu ngắn hơn."));
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email)) return Conflict(ApiResponse<object>.Fail("Email đã được sử dụng."));
        var user = new User { Email = email, Password = BCrypt.Net.BCrypt.HashPassword(dto.Password), FullName = dto.FullName.Trim(), PhoneNumber = dto.PhoneNumber, Role = dto.Role };
        // An academic email address alone does not prove mailbox ownership.
        user.IsEduVerified = false;
        db.Users.Add(user);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(ApiResponse<object>.Fail("Email đã được sử dụng.")); }
        return Ok(ApiResponse<AuthResponseDto>.Ok(new() { Token = tokens.GenerateToken(user), User = Mapping.Profile(user) }));
    }
    [HttpPost("login"), EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginRequestDto dto)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(dto.Password) > 72)
            return Unauthorized(ApiResponse<object>.Fail("Email hoặc mật khẩu không chính xác."));
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
            return Unauthorized(ApiResponse<object>.Fail("Email hoặc mật khẩu không chính xác."));
        return Ok(ApiResponse<AuthResponseDto>.Ok(new() { Token = tokens.GenerateToken(user), User = Mapping.Profile(user) }));
    }
    [Authorize, HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await db.Users.FindAsync(User.UserId());
        return user == null ? Unauthorized() : Ok(ApiResponse<UserProfileDto>.Ok(Mapping.Profile(user)));
    }
    [Authorize, HttpPut("me")]
    public async Task<IActionResult> Update(ProfileUpdate dto)
    {
        var user = await db.Users.FindAsync(User.UserId());
        if (user == null) return Unauthorized();
        user.FullName = dto.FullName.Trim(); user.PhoneNumber = dto.PhoneNumber; user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<UserProfileDto>.Ok(Mapping.Profile(user)));
    }

    [Authorize, HttpPut("password"), EnableRateLimiting("auth")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var user = await db.Users.FindAsync(User.UserId());
        if (user == null) return Unauthorized();
        if (System.Text.Encoding.UTF8.GetByteCount(dto.CurrentPassword) > 72 ||
            !BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.Password))
            return BadRequest(ApiResponse<object>.Fail("Mật khẩu hiện tại không chính xác."));
        if (System.Text.Encoding.UTF8.GetByteCount(dto.NewPassword) > 72)
            return BadRequest(ApiResponse<object>.Fail("Mật khẩu mới không được vượt quá 72 byte."));
        if (dto.NewPassword == dto.CurrentPassword)
            return BadRequest(ApiResponse<object>.Fail("Mật khẩu mới cần khác mật khẩu hiện tại."));
        user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<AuthResponseDto>.Ok(new() { Token = tokens.GenerateToken(user), User = Mapping.Profile(user) }));
    }
}
public record ProfileUpdate([System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(150)] string FullName, [System.ComponentModel.DataAnnotations.MaxLength(20)] string? PhoneNumber);
public record ChangePasswordDto(
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(72)] string CurrentPassword,
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MinLength(8), System.ComponentModel.DataAnnotations.MaxLength(72)] string NewPassword);


