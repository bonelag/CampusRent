using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.DTOs.Reports;
using CampusRent.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CampusRent.Server.Controllers;
[ApiController, Route("api/admin"), Authorize(Roles = "ADMIN")]
public class AdminController(CampusRentDbContext db) : ControllerBase
{
    [HttpGet("stats")] public async Task<IActionResult> Stats() => Ok(ApiResponse<object>.Ok(new { users = await db.Users.CountAsync(), rooms = await db.Rooms.CountAsync(), availableRooms = await db.Rooms.CountAsync(r => r.Status == RoomStatus.AVAILABLE && r.ExpiresAt > DateTime.UtcNow), pendingReports = await db.Reports.CountAsync(r => r.Status == ReportStatus.PENDING), flaggedReviews = await db.Reviews.CountAsync(r => r.IsFlagged), bookings = await db.Bookings.CountAsync() }));
    [HttpGet("reports")] public async Task<IActionResult> Reports() => Ok(ApiResponse<object>.Ok(await db.Reports.Include(r => r.Room).Include(r => r.Review).Include(r => r.Reporter).OrderByDescending(r => r.CreatedAt).Select(r => new { r.Id, r.RoomId, r.ReviewId, r.Reason, r.Description, r.Status, r.CreatedAt, roomTitle = r.Room == null ? null : r.Room.Title, reviewComment = r.Review == null ? null : r.Review.Comment, reporterName = r.Reporter.FullName }).ToListAsync()));
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
    [HttpGet("reviews")] public async Task<IActionResult> Reviews() => Ok(ApiResponse<object>.Ok(await db.Reviews.Where(r => r.IsFlagged).Include(r => r.User).Include(r => r.Room).Select(r => new { r.Id, r.Comment, r.Rating, r.FlagReason, roomTitle = r.Room.Title, userName = r.User.FullName }).ToListAsync()));
    [HttpPatch("reviews/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var review = await db.Reviews.FindAsync(id); if (review == null) return NotFound();
        review.IsFlagged = false; review.FlagReason = null; await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { review.Id }));
    }
}
