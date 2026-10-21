using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.DTOs.Reports;
using CampusRent.Server.Models;
using CampusRent.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CampusRent.Server.Controllers;
[ApiController, Route("api/reports"), Authorize]
public class ReportsController(CampusRentDbContext db) : ControllerBase
{
    [HttpPost] public async Task<IActionResult> Create(CreateReportDto dto)
    {
        if (dto.RoomId.HasValue == dto.ReviewId.HasValue) return BadRequest(ApiResponse<object>.Fail("Chọn đúng một tin hoặc đánh giá cần báo cáo."));
        if (dto.RoomId.HasValue && !await db.Rooms.AnyAsync(r => r.Id == dto.RoomId) || dto.ReviewId.HasValue && !await db.Reviews.AnyAsync(r => r.Id == dto.ReviewId)) return NotFound();
        var report = new Report { ReporterId = User.UserId(), RoomId = dto.RoomId, ReviewId = dto.ReviewId, Reason = dto.Reason.Trim(), Description = dto.Description };
        db.Reports.Add(report); await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { report.Id }, "Đã gửi báo cáo cho quản trị viên."));
    }
}
