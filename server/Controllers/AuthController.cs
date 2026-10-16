using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Auth;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusRent.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly CampusRentDbContext _dbContext;
    private readonly ITokenService _tokenService;

    public AuthController(CampusRentDbContext dbContext, ITokenService tokenService)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Đăng ký tài khoản người dùng mới
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterRequestDto dto)
    {
        // TODO: Thành viên nhóm triển khai:
        // 1. Kiểm tra email đã tồn tại trong CSDL chưa
        // 2. Kiểm tra nếu email có đuôi ".edu.vn" -> gán IsEduVerified = true
        // 3. Hash mật khẩu bằng BCrypt
        // 4. Lưu User mới vào CSDL
        // 5. Sinh JWT token qua _tokenService và trả về AuthResponseDto
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented, ApiResponse<AuthResponseDto>.Fail("Chưa triển khai logic Register"));
    }

    /// <summary>
    /// Đăng nhập tài khoản
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginRequestDto dto)
    {
        // TODO: Thành viên nhóm triển khai:
        // 1. Tìm User theo email trong CSDL
        // 2. Xác thực mật khẩu bằng BCrypt.Verify
        // 3. Tạo JWT token qua _tokenService và trả về thông tin đăng nhập
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented, ApiResponse<AuthResponseDto>.Fail("Chưa triển khai logic Login"));
    }

    /// <summary>
    /// Lấy thông tin tài khoản hiện tại từ Token
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetMe()
    {
        // TODO: Thành viên nhóm triển khai:
        // 1. Đọc UserId từ ClaimTypes.NameIdentifier trong User context
        // 2. Tìm thông tin User trong CSDL và map sang UserProfileDto
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented, ApiResponse<UserProfileDto>.Fail("Chưa triển khai logic GetMe"));
    }
}
