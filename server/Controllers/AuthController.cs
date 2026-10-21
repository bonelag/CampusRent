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
        if (dto.Role is not (Role.STUDENT or Role.LANDLORD)) return BadRequest(ApiResponse<object>.Fail("Vai trò đăng ký không hợp lệ."));
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
}
public record ProfileUpdate([System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(150)] string FullName, [System.ComponentModel.DataAnnotations.MaxLength(20)] string? PhoneNumber);


