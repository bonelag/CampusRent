using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.DTOs.Reviews;
using CampusRent.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusRent.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly CampusRentDbContext _dbContext;
    private readonly IProfanityFilterService _profanityFilter;

    public ReviewsController(CampusRentDbContext dbContext, IProfanityFilterService profanityFilter)
    {
        _dbContext = dbContext;
        _profanityFilter = profanityFilter;
    }

    /// <summary>
    /// Viết nhận xét / đánh giá cho phòng trọ (kèm kiểm duyệt từ ngữ cấm)
    /// </summary>
    [HttpPost("/api/rooms/{roomId}/reviews")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateReview(Guid roomId, [FromBody] CreateReviewDto dto)
    {
        // TODO: Thành viên nhóm triển khai:
        // 1. Lấy UserId từ User context
        // 2. Kiểm tra xem người dùng đã từng đánh giá phòng này chưa (mỗi user chỉ đánh giá 1 lần/phòng)
        // 3. Sử dụng _profanityFilter để kiểm tra từ ngữ hạn chế trong dto.Comment
        // 4. Nếu dính từ cấm -> đánh dấu IsFlagged = true
        // 5. Lưu Review vào CSDL và trả về kết quả
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented, ApiResponse<Guid>.Fail("Chưa triển khai logic CreateReview"));
    }
}
