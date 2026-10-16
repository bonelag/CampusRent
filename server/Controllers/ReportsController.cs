using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.DTOs.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusRent.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly CampusRentDbContext _dbContext;

    public ReportsController(CampusRentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Báo cáo bài đăng phòng trọ ảo hoặc đánh giá bôi nhọ vi phạm
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateReport([FromBody] CreateReportDto dto)
    {
        // TODO: Thành viên nhóm triển khai:
        // 1. Lấy ReporterId từ User context
        // 2. Validate rằng có ít nhất một đối tượng bị báo cáo (RoomId hoặc ReviewId)
        // 3. Tạo bản ghi Report với Status = PENDING và lưu vào CSDL
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented, ApiResponse<Guid>.Fail("Chưa triển khai logic CreateReport"));
    }
}
