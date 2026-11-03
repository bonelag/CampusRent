using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.DTOs.LandlordRequests;
using CampusRent.Server.Models;
using CampusRent.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusRent.Server.Controllers;

[ApiController, Route("api/landlord-requests"), Authorize]
public class LandlordRequestsController(CampusRentDbContext db) : ControllerBase
{
    [HttpGet("my")]
    public async Task<IActionResult> MyRequests()
    {
        var userId = User.UserId();
        var requests = await db.LandlordRequests
            .Include(r => r.User)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => Mapping.LandlordRequest(r))
            .ToListAsync();

        return Ok(ApiResponse<List<LandlordRequestResponseDto>>.Ok(requests));
    }

    [HttpPost]
    public async Task<IActionResult> Submit(CreateLandlordRequestDto dto)
    {
        var userId = User.UserId();
        var user = await db.Users.FindAsync(userId);
        if (user == null) return Unauthorized();

        if (user.Role != Role.STUDENT)
            return BadRequest(ApiResponse<object>.Fail("Tài khoản của bạn đã là chủ trọ hoặc quản trị viên."));

        var hasPending = await db.LandlordRequests
            .AnyAsync(r => r.UserId == userId && r.Status == LandlordRequestStatus.PENDING);
        if (hasPending)
            return BadRequest(ApiResponse<object>.Fail("Bạn đã gửi một yêu cầu và đang chờ quản trị viên xét duyệt."));

        var req = new LandlordRequest
        {
            UserId = userId,
            Title = dto.Title.Trim(),
            Reason = dto.Reason.Trim(),
            ImageUrls = dto.ImageUrls?.Take(10).ToList() ?? new List<string>(),
            PdfUrl = string.IsNullOrWhiteSpace(dto.PdfUrl) ? null : dto.PdfUrl.Trim(),
            PdfFileName = string.IsNullOrWhiteSpace(dto.PdfFileName) ? null : dto.PdfFileName.Trim(),
            Status = LandlordRequestStatus.PENDING,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.LandlordRequests.Add(req);
        await db.SaveChangesAsync();

        req.User = user;
        return Created($"/api/landlord-requests/{req.Id}", ApiResponse<LandlordRequestResponseDto>.Ok(Mapping.LandlordRequest(req)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = User.UserId();
        var req = await db.LandlordRequests.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (req == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy yêu cầu."));

        if (req.Status != LandlordRequestStatus.PENDING)
            return BadRequest(ApiResponse<object>.Fail("Chỉ có thể hủy yêu cầu đang ở trạng thái chờ duyệt."));

        db.LandlordRequests.Remove(req);
        await db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { message = "Đã hủy yêu cầu nâng cấp." }));
    }
}
