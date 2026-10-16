using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.DTOs.Reports;
using CampusRent.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusRent.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(Role.ADMIN))]
public class AdminController : ControllerBase
{
    private readonly CampusRentDbContext _dbContext;

    public AdminController(CampusRentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Thống kê tổng quan hệ thống (User, Room, Review, Report)
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<object>>> GetStats()
    {
        // TODO: Thành viên nhóm triển khai:
        // Đếm tổng số lượng người dùng (sinh viên, chủ trọ), phòng trọ theo trạng thái, báo cáo chờ duyệt
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented, ApiResponse<object>.Fail("Chưa triển khai logic GetStats"));
    }

    /// <summary>
    /// Lấy danh sách các báo cáo vi phạm
    /// </summary>
    [HttpGet("reports")]
    public async Task<ActionResult<ApiResponse<List<ReportItemDto>>>> GetReports([FromQuery] ReportStatus? status)
    {
        // TODO: Thành viên nhóm triển khai:
        // Lấy danh sách báo cáo vi phạm kèm thông tin người báo cáo, bài đăng hoặc bình luận liên quan
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented, ApiResponse<List<ReportItemDto>>.Fail("Chưa triển khai logic GetReports"));
    }

    /// <summary>
    /// Xử lý duyệt hoặc bác bỏ báo cáo vi phạm
    /// </summary>
    [HttpPatch("reports/{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> ResolveReport(Guid id, [FromBody] ResolveReportDto dto)
    {
        // TODO: Thành viên nhóm triển khai:
        // 1. Cập nhật trạng thái báo cáo (RESOLVED hoặc DISMISSED)
        // 2. Nếu DeleteTarget == true: xóa bài đăng hoặc bình luận vi phạm tương ứng
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented, ApiResponse<bool>.Fail("Chưa triển khai logic ResolveReport"));
    }
}
