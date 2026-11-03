using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.DTOs.LandlordRequests;
using CampusRent.Server.DTOs.Reports;
using CampusRent.Server.Models;
using CampusRent.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CampusRent.Server.Controllers;
[ApiController, Route("api/admin"), Authorize(Roles = "ADMIN")]
public class AdminController(CampusRentDbContext db) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> Stats() => Ok(ApiResponse<object>.Ok(new
    {
        users = await db.Users.CountAsync(),
        rooms = await db.Rooms.CountAsync(),
        availableRooms = await db.Rooms.CountAsync(r => r.Status == RoomStatus.AVAILABLE && r.ExpiresAt > DateTime.UtcNow),
        pendingReports = await db.Reports.CountAsync(r => r.Status == ReportStatus.PENDING),
        flaggedReviews = await db.Reviews.CountAsync(r => r.IsFlagged),
        bookings = await db.Bookings.CountAsync(),
        pendingLandlordRequests = await db.LandlordRequests.CountAsync(r => r.Status == LandlordRequestStatus.PENDING)
    }));

    [HttpGet("reports")]
    public async Task<IActionResult> Reports() => Ok(ApiResponse<object>.Ok(await db.Reports.Include(r => r.Room).Include(r => r.Review).Include(r => r.Reporter).OrderByDescending(r => r.CreatedAt).Select(r => new { r.Id, r.RoomId, r.ReviewId, r.Reason, r.Description, r.Status, r.CreatedAt, roomTitle = r.Room == null ? null : r.Room.Title, reviewComment = r.Review == null ? null : r.Review.Comment, reporterName = r.Reporter.FullName }).ToListAsync()));

    [HttpPatch("reports/{id:guid}")]
    public async Task<IActionResult> Resolve(Guid id, ResolveReportDto dto)
    {
        if (dto.Status is not (ReportStatus.RESOLVED or ReportStatus.DISMISSED) || dto.DeleteTarget && dto.Status != ReportStatus.RESOLVED) return BadRequest();
        var report = await db.Reports.Include(r => r.Room).Include(r => r.Review).SingleOrDefaultAsync(r => r.Id == id);
        if (report == null) return NotFound();
        if (report.Status != ReportStatus.PENDING) return Conflict(ApiResponse<object>.Fail("Báo cáo đã được xử lý."));
        report.Status = dto.Status;
        if (dto.DeleteTarget) { if (report.Room != null) report.Room.Status = RoomStatus.HIDDEN; if (report.Review != null) report.Review.IsFlagged = true; }
        await db.SaveChangesAsync(); return Ok(ApiResponse<object>.Ok(new { report.Id, report.Status }));
    }

    [HttpGet("reviews")]
    public async Task<IActionResult> Reviews() => Ok(ApiResponse<object>.Ok(await db.Reviews.Where(r => r.IsFlagged).Include(r => r.User).Include(r => r.Room).Select(r => new { r.Id, r.Comment, r.Rating, r.FlagReason, roomTitle = r.Room.Title, userName = r.User.FullName }).ToListAsync()));

    [HttpPatch("reviews/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var review = await db.Reviews.FindAsync(id); if (review == null) return NotFound();
        review.IsFlagged = false; review.FlagReason = null; await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { review.Id }));
    }

    [HttpGet("landlord-requests")]
    public async Task<IActionResult> LandlordRequests()
    {
        var requests = await db.LandlordRequests
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => Mapping.LandlordRequest(r))
            .ToListAsync();
        return Ok(ApiResponse<List<LandlordRequestResponseDto>>.Ok(requests));
    }

    [HttpPatch("landlord-requests/{id:guid}/approve")]
    public async Task<IActionResult> ApproveLandlordRequest(Guid id)
    {
        var req = await db.LandlordRequests.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == id);
        if (req == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy yêu cầu."));
        if (req.Status != LandlordRequestStatus.PENDING) return Conflict(ApiResponse<object>.Fail("Yêu cầu đã được xử lý trước đó."));

        req.Status = LandlordRequestStatus.APPROVED;
        req.UpdatedAt = DateTime.UtcNow;
        req.User.Role = Role.LANDLORD;
        req.User.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { req.Id, status = req.Status, userRole = req.User.Role }));
    }

    [HttpPatch("landlord-requests/{id:guid}/reject")]
    public async Task<IActionResult> RejectLandlordRequest(Guid id, RejectLandlordRequestDto? dto)
    {
        var req = await db.LandlordRequests.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == id);
        if (req == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy yêu cầu."));
        if (req.Status != LandlordRequestStatus.PENDING) return Conflict(ApiResponse<object>.Fail("Yêu cầu đã được xử lý trước đó."));

        req.Status = LandlordRequestStatus.REJECTED;
        req.AdminNote = dto?.AdminNote?.Trim();
        req.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { req.Id, status = req.Status }));
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery] string? q)
    {
        var query = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(term) || u.Email.ToLower().Contains(term) || (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FullName,
                u.PhoneNumber,
                u.Avatar,
                u.Role,
                u.IsEduVerified,
                u.CreatedAt,
                u.UpdatedAt,
                RoomsCount = u.Rooms.Count,
                ReviewsCount = u.Reviews.Count
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(users));
    }

    [HttpPut("users/{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, AdminUpdateUserDto dto)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy người dùng."));

        user.FullName = dto.FullName.Trim();
        user.PhoneNumber = dto.PhoneNumber?.Trim();
        user.Role = dto.Role;
        user.IsEduVerified = dto.IsEduVerified;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new
        {
            user.Id,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.Role,
            user.IsEduVerified,
            user.UpdatedAt
        }));
    }
}

public record AdminUpdateUserDto(
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(150)] string FullName,
    [System.ComponentModel.DataAnnotations.MaxLength(20)] string? PhoneNumber,
    Role Role,
    bool IsEduVerified
);
