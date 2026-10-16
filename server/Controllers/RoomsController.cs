using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.DTOs.Rooms;
using CampusRent.Server.Models;
using CampusRent.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusRent.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly CampusRentDbContext _dbContext;
    private readonly IHaversineService _haversineService;
    private readonly ILivingCostService _livingCostService;
    private readonly ICloudinaryService _cloudinaryService;

    public RoomsController(
        CampusRentDbContext dbContext,
        IHaversineService haversineService,
        ILivingCostService livingCostService,
        ICloudinaryService cloudinaryService)
    {
        _dbContext = dbContext;
        _haversineService = haversineService;
        _livingCostService = livingCostService;
        _cloudinaryService = cloudinaryService;
    }

    /// <summary>
    /// Tìm kiếm và lọc danh sách phòng trọ theo trường ĐH, bán kính (Haversine), giá, v.v.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RoomListItemDto>>>> GetRooms([FromQuery] RoomQueryParameters query)
    {
        // TODO: Thành viên nhóm triển khai:
        // 1. Lọc phòng theo trạng thái AVAILABLE, khoảng giá, từ khóa tìm kiếm
        // 2. Nếu có UniversityId hoặc tọa độ tâm: tính khoảng cách bằng _haversineService
        // 3. Lọc bán kính (Radius: 1km, 3km, 5km)
        // 4. Sắp xếp theo cự ly gần nhất hoặc ngày đăng mới nhất
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented, ApiResponse<List<RoomListItemDto>>.Fail("Chưa triển khai logic GetRooms"));
    }

    /// <summary>
    /// Lấy chi tiết thông tin phòng trọ, danh sách ảnh, thông tin chủ nhà và đánh giá
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RoomDetailDto>>> GetById(Guid id)
    {
        // TODO: Thành viên nhóm triển khai:
        // 1. Tìm phòng trọ theo ID (Include Landlord, University, Images, Reviews)
        // 2. Tính khoảng cách từ phòng đến trường ĐH liên kết
        // 3. Tính điểm đánh giá trung bình (AverageRating) và trả về RoomDetailDto
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented, ApiResponse<RoomDetailDto>.Fail("Chưa triển khai logic GetById room"));
    }

    /// <summary>
    /// Đăng tin phòng trọ mới (Chủ trọ / Admin)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{nameof(Role.LANDLORD)},{nameof(Role.ADMIN)}")]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateRoomDto dto)
    {
        // TODO: Thành viên nhóm triển khai:
        // 1. Lấy LandlordId từ User Claims
        // 2. Tạo đối tượng Room mới với thời hạn ExpiresAt = DateTime.UtcNow.AddDays(30)
        // 3. Gắn danh sách hình ảnh đính kèm (nếu có)
        // 4. Lưu vào CSDL và trả về ID phòng vừa tạo
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented, ApiResponse<Guid>.Fail("Chưa triển khai logic Create room"));
    }

    /// <summary>
    /// Cập nhật thông tin phòng trọ hoặc gia hạn thêm 30 ngày
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = $"{nameof(Role.LANDLORD)},{nameof(Role.ADMIN)}")]
    public async Task<ActionResult<ApiResponse<bool>>> Update(Guid id, [FromBody] UpdateRoomDto dto)
    {
        // TODO: Thành viên nhóm triển khai:
        // 1. Kiểm tra quyền sở hữu phòng trọ (Chính chủ trọ hoặc Admin)
        // 2. Cập nhật các trường thông tin được gửi lên
        // 3. Nếu RenewExpires == true: cộng thêm 30 ngày vào ExpiresAt và set Status = AVAILABLE
        // 4. Lưu thay đổi vào CSDL
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented, ApiResponse<bool>.Fail("Chưa triển khai logic Update room"));
    }

    /// <summary>
    /// Báo cáo phòng đã hết / đã cho thuê
    /// </summary>
    [HttpPatch("{id}/report-full")]
    public async Task<ActionResult<ApiResponse<bool>>> ReportRoomFull(Guid id)
    {
        // TODO: Thành viên nhóm triển khai:
        // Ghi nhận phản hồi phòng đã hết -> chuyển trạng thái Status = RENTED hoặc ghi nhận cờ cảnh báo
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented, ApiResponse<bool>.Fail("Chưa triển khai logic ReportRoomFull"));
    }

    /// <summary>
    /// Dự toán chi phí sinh hoạt hàng tháng dựa trên đơn giá và mức sử dụng
    /// </summary>
    [HttpPost("estimate-cost")]
    public ActionResult<ApiResponse<CostEstimateDto>> EstimateCost(
        [FromQuery] decimal basePrice,
        [FromQuery] decimal electricPrice,
        [FromQuery] decimal waterPrice,
        [FromQuery] decimal internetPrice,
        [FromQuery] decimal otherFee,
        [FromQuery] decimal expectedKwh = 80,
        [FromQuery] decimal expectedM3 = 4)
    {
        // TODO: Thành viên nhóm gọi _livingCostService để tính tổng chi phí sinh hoạt
        return StatusCode(StatusCodes.Status501NotImplemented, ApiResponse<CostEstimateDto>.Fail("Chưa triển khai logic EstimateCost"));
    }

    /// <summary>
    /// Tải ảnh phòng trọ lên Cloudinary
    /// </summary>
    [HttpPost("{id}/images")]
    [Authorize(Roles = $"{nameof(Role.LANDLORD)},{nameof(Role.ADMIN)}")]
    public async Task<ActionResult<ApiResponse<string>>> UploadImage(Guid id, IFormFile file)
    {
        // TODO: Thành viên nhóm gọi _cloudinaryService để upload ảnh và lưu bản ghi RoomImage vào CSDL
        await Task.CompletedTask;
        return StatusCode(StatusCodes.Status501NotImplemented, ApiResponse<string>.Fail("Chưa triển khai logic UploadImage"));
    }
}
