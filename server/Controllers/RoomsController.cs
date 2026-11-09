using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.DTOs.Rooms;
using CampusRent.Server.Models;
using CampusRent.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CampusRent.Server.Controllers;
[ApiController, Route("api/rooms")]
public class RoomsController(CampusRentDbContext db, IHaversineService haversine, ILivingCostService costs) : ControllerBase
{
    private IQueryable<Room> Loaded => db.Rooms.Include(r => r.Images).Include(r => r.Reviews).AsSplitQuery();
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] RoomQueryParameters q)
    {
        var query = Loaded.AsNoTracking().Where(r => r.Status == RoomStatus.AVAILABLE && r.ExpiresAt > DateTime.UtcNow);
        if (q.MinPrice.HasValue) query = query.Where(r => r.BasePrice >= q.MinPrice.Value);
        if (q.MaxPrice.HasValue) query = query.Where(r => r.BasePrice <= q.MaxPrice.Value);
        if (!string.IsNullOrWhiteSpace(q.Keyword)) { var keyword = q.Keyword.Trim().ToLower(); query = query.Where(r => r.Title.ToLower().Contains(keyword) || r.Address.ToLower().Contains(keyword)); }
        double? lat = q.CenterLat, lon = q.CenterLon;
        if (q.UniversityId.HasValue)
        {
            var university = await db.Universities.FindAsync(q.UniversityId);
            if (university == null) return BadRequest(ApiResponse<object>.Fail("Trường đại học không tồn tại."));
            lat = university.Latitude; lon = university.Longitude;
        }
        if (lat.HasValue != lon.HasValue || (q.Radius.HasValue && !lat.HasValue))
            return BadRequest(ApiResponse<object>.Fail("Chọn trường hoặc tọa độ trung tâm để lọc bán kính."));
        var rooms = await query.ToListAsync();
        var items = rooms.Select(r => Mapping.Card(r, lat.HasValue ? haversine.CalculateDistanceKm(lat.Value, lon!.Value, r.Latitude, r.Longitude) : null)).AsEnumerable();
        if (q.Radius.HasValue) items = items.Where(r => r.DistanceKm <= q.Radius);
        if (!string.IsNullOrWhiteSpace(q.Amenity)) items = items.Where(r => r.Amenities.Contains(q.Amenity));
        items = q.Sort switch { "price_asc" => items.OrderBy(r => r.BasePrice), "price_desc" => items.OrderByDescending(r => r.BasePrice), "distance" => items.OrderBy(r => r.DistanceKm ?? double.MaxValue), _ => items.OrderByDescending(r => r.CreatedAt) };
        var list = items.ToList();
        return Ok(ApiResponse<object>.Ok(new { items = list.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize), total = list.Count, q.Page, q.PageSize }));
    }
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var r = await Loaded.Include(r => r.Landlord).Include(r => r.University).Include(r => r.Reviews).ThenInclude(v => v.User).SingleOrDefaultAsync(r => r.Id == id);
        if (r == null || ((r.Status != RoomStatus.AVAILABLE || r.ExpiresAt <= DateTime.UtcNow) && !CanManage(r))) return NotFound(ApiResponse<object>.Fail("Tin không tồn tại hoặc đã ngừng hiển thị."));
        var reviews = r.Reviews.Where(v => !v.IsFlagged).OrderByDescending(v => v.CreatedAt).ToList();
        return Ok(ApiResponse<object>.Ok(new { r.Id, r.Title, r.Description, r.Address, r.Latitude, r.Longitude, r.BasePrice, r.ElectricPrice, r.WaterPrice, r.InternetPrice, r.OtherFee, r.Area, r.MaxPeople, r.Amenities, r.Status, r.ExpiresAt, r.CreatedAt, r.UniversityId,
            images = r.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url),
            landlord = new { r.Landlord.Id, r.Landlord.FullName, r.Landlord.PhoneNumber },
            university = r.University == null ? null : new { r.University.Id, r.University.Name, r.University.ShortName, r.University.Latitude, r.University.Longitude },
            distanceToUniversityKm = r.University == null ? (double?)null : haversine.CalculateDistanceKm(r.Latitude, r.Longitude, r.University.Latitude, r.University.Longitude),
            averageRating = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(v => v.Rating), 1), reviewCount = reviews.Count,
            reviews = reviews.Select(v => new { v.Id, v.Rating, v.Comment, v.CreatedAt, user = new { v.User.FullName, v.User.IsEduVerified } })
        }));
    }
    [HttpGet("{id:guid}/cost-estimate")]
    public async Task<IActionResult> Estimate(Guid id, [FromQuery, System.ComponentModel.DataAnnotations.Range(0, 10000)] decimal expectedKwh = 80, [FromQuery, System.ComponentModel.DataAnnotations.Range(0, 1000)] decimal expectedM3 = 4)
    {
        var r = await db.Rooms.FindAsync(id);
        if (r == null || r.Status != RoomStatus.AVAILABLE || r.ExpiresAt <= DateTime.UtcNow) return NotFound();
        return Ok(ApiResponse<object>.Ok(new { electricCost = r.ElectricPrice * expectedKwh, waterCost = r.WaterPrice * expectedM3, totalEstimatedMonthlyCost = costs.CalculateMonthlyCost(r.BasePrice, r.ElectricPrice, r.WaterPrice, r.InternetPrice, r.OtherFee, expectedKwh, expectedM3) }));
    }
    [HttpGet("mine"), Authorize(Roles = "LANDLORD,ADMIN")]
    public async Task<IActionResult> Mine()
    {
        var rooms = await Loaded.Where(r => r.LandlordId == User.UserId()).ToListAsync();
        return Ok(ApiResponse<object>.Ok(rooms.OrderByDescending(r => r.CreatedAt).Select(r => Mapping.Card(r))));
    }
    [HttpPost, Authorize(Roles = "LANDLORD,ADMIN")]
    public async Task<IActionResult> Create(CreateRoomDto dto)
    {
        var error = await Validate(dto); if (error != null) return BadRequest(ApiResponse<object>.Fail(error));
        var r = new Room { LandlordId = User.UserId() };
        Apply(r, dto); db.Rooms.Add(r); await db.SaveChangesAsync();
        return Created($"/api/rooms/{r.Id}", ApiResponse<object>.Ok(new { r.Id }));
    }
    [HttpPut("{id:guid}"), Authorize(Roles = "LANDLORD,ADMIN")]
    public async Task<IActionResult> Update(Guid id, CreateRoomDto dto)
    {
        var r = await Loaded.SingleOrDefaultAsync(r => r.Id == id);
        if (r == null) return NotFound(); if (!CanManage(r)) return Forbid();
        var error = await Validate(dto); if (error != null) return BadRequest(ApiResponse<object>.Fail(error));
        db.RoomImages.RemoveRange(r.Images); Apply(r, dto); db.RoomImages.AddRange(r.Images); r.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(); return Ok(ApiResponse<object>.Ok(new { r.Id }));
    }
    [HttpPatch("{id:guid}/status"), Authorize(Roles = "LANDLORD,ADMIN")]
    public async Task<IActionResult> Status(Guid id, RoomStatusUpdate dto)
    {
        var r = await db.Rooms.FindAsync(id); if (r == null) return NotFound(); if (!CanManage(r)) return Forbid();
        if (!Enum.IsDefined(dto.Status)) return BadRequest();
        r.Status = dto.Status; r.UpdatedAt = DateTime.UtcNow;
        if (dto.Renew) r.ExpiresAt = DateTime.UtcNow.AddDays(30);
        await db.SaveChangesAsync(); return Ok(ApiResponse<object>.Ok(new { r.Id, r.Status, r.ExpiresAt }));
    }
    [HttpDelete("{id:guid}"), Authorize(Roles = "LANDLORD,ADMIN")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var r = await db.Rooms.FindAsync(id); if (r == null) return NotFound(); if (!CanManage(r)) return Forbid();
        // Archive to preserve booking/report history.
        r.Status = RoomStatus.HIDDEN; r.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { r.Id }));
    }
    private bool CanManage(Room r) => User.Identity?.IsAuthenticated == true && (User.IsInRole("ADMIN") || r.LandlordId == User.UserId());
    private async Task<string?> Validate(CreateRoomDto dto)
    {
        if (dto.UniversityId.HasValue && !await db.Universities.AnyAsync(u => u.Id == dto.UniversityId)) return "Trường đại học không hợp lệ.";
        if (dto.ImageUrls.Count is < 1 or > 10 || dto.ImageUrls.Any(url => !UploadStorage.IsPublicImageUrl(url) && (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https"))) return "Cần 1–10 ảnh đã tải lên hoặc đường dẫn HTTPS.";
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Description) || string.IsNullOrWhiteSpace(dto.Address)) return "Nhập đầy đủ tiêu đề, mô tả và địa chỉ.";
        if (dto.Amenities.Count > 20 || dto.Amenities.Any(a => a.Length > 50)) return "Danh sách tiện ích không hợp lệ.";
        return null;
    }
    private static void Apply(Room r, CreateRoomDto dto)
    {
        r.UniversityId = dto.UniversityId; r.Title = dto.Title.Trim(); r.Description = dto.Description.Trim(); r.Address = dto.Address.Trim(); r.Latitude = dto.Latitude; r.Longitude = dto.Longitude;
        r.BasePrice = dto.BasePrice; r.ElectricPrice = dto.ElectricPrice; r.WaterPrice = dto.WaterPrice; r.InternetPrice = dto.InternetPrice; r.OtherFee = dto.OtherFee; r.Area = dto.Area; r.MaxPeople = dto.MaxPeople; r.Amenities = dto.Amenities.Distinct().ToList();
        r.Images = dto.ImageUrls.Select((url, i) => new RoomImage { Url = url, IsCover = i == 0, DisplayOrder = i }).ToList();
    }
}
public record RoomStatusUpdate(RoomStatus Status, bool Renew = false);

