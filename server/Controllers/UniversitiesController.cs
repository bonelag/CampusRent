using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.DTOs.Universities;
using CampusRent.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusRent.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UniversitiesController : ControllerBase
{
    private readonly CampusRentDbContext _dbContext;

    public UniversitiesController(CampusRentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Lấy danh sách tất cả các trường Đại học
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UniversityDto>>>> GetAll()
    {
        // TODO: Thành viên nhóm triển khai:
        // Truy vấn danh sách trường đại học từ _dbContext.Universities và map sang List<UniversityDto>
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented, ApiResponse<List<UniversityDto>>.Fail("Chưa triển khai logic GetAll universities"));
    }

    /// <summary>
    /// Thêm mới trường Đại học (Dành cho Admin)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = nameof(Role.ADMIN))]
    public async Task<ActionResult<ApiResponse<UniversityDto>>> Create([FromBody] CreateUniversityDto dto)
    {
        // TODO: Thành viên nhóm triển khai:
        // 1. Validate dữ liệu đầu vào (tên trường, mã viết tắt, tọa độ Lat/Lon)
        // 2. Tạo thực thể University và lưu vào CSDL
        // 3. Trả về thông tin trường vừa tạo
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented, ApiResponse<UniversityDto>.Fail("Chưa triển khai logic Create university"));
    }
}
