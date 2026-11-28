using System.ComponentModel.DataAnnotations;
using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.DTOs.Rooms;
using CampusRent.Server.Models;
using CampusRent.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusRent.Server.Controllers;

[ApiController]
[Route("api/rooms")]
public class RoomsController(
    CampusRentDbContext db,
    IHaversineService haversine,
    ILivingCostService costs,
    IReviewRankingService rankingService) : ControllerBase
{
    private IQueryable<Room> RoomsWithDetails => db.Rooms
        .Include(r => r.Images)
        .Include(r => r.Reviews)
        .ThenInclude(v => v.User)
        .AsSplitQuery();

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] RoomQueryParameters q)
    {
        var query = RoomsWithDetails
            .AsNoTracking()
            .Where(r => r.Status == RoomStatus.AVAILABLE && r.ExpiresAt > DateTime.UtcNow);

        if (q.MinPrice.HasValue)
        {
            query = query.Where(r => r.BasePrice >= q.MinPrice.Value);
        }

        if (q.MaxPrice.HasValue)
        {
            query = query.Where(r => r.BasePrice <= q.MaxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            var keyword = q.Keyword.Trim().ToLower();
            query = query.Where(r => r.Title.ToLower().Contains(keyword) || r.Address.ToLower().Contains(keyword));
        }

        double? lat = q.CenterLat;
        double? lon = q.CenterLon;

        if (q.UniversityId.HasValue)
        {
            var university = await db.Universities.FindAsync(q.UniversityId);
            if (university == null)
            {
                return BadRequest(ApiResponse<object>.Fail("Trường đại học không tồn tại."));
            }

            lat = university.Latitude;
            lon = university.Longitude;
        }

        if (lat.HasValue != lon.HasValue || (q.Radius.HasValue && !lat.HasValue))
        {
            return BadRequest(ApiResponse<object>.Fail("Chọn trường hoặc tọa độ trung tâm để lọc bán kính."));
        }

        var rooms = await query.ToListAsync();
        var landlordIds = rooms.Select(r => r.LandlordId).Distinct().ToList();
        var landlordReviews = await db.Reviews
            .AsNoTracking()
            .Include(v => v.User)
            .Include(v => v.Room)
            .Where(v => !v.IsFlagged && landlordIds.Contains(v.Room.LandlordId))
            .ToListAsync();
        var landlordReviewsLookup = landlordReviews.ToLookup(v => v.Room.LandlordId);

        var items = rooms.Select(r =>
        {
            double? distanceKm = lat.HasValue
                ? haversine.CalculateDistanceKm(lat.Value, lon!.Value, r.Latitude, r.Longitude)
                : null;
            return Mapping.Card(r, distanceKm, rankingService, landlordReviewsLookup[r.LandlordId]);
        });

        if (q.Radius.HasValue)
        {
            items = items.Where(r => r.DistanceKm <= q.Radius);
        }

        if (!string.IsNullOrWhiteSpace(q.Amenity))
        {
            items = items.Where(r => r.Amenities.Contains(q.Amenity));
        }

        items = q.Sort switch
        {
            "price_asc" => items.OrderBy(r => r.IsDemoted ? 1 : 0).ThenBy(r => r.BasePrice),
            "price_desc" => items.OrderBy(r => r.IsDemoted ? 1 : 0).ThenByDescending(r => r.BasePrice),
            "distance" => items.OrderBy(r => r.IsDemoted ? 1 : 0).ThenBy(r => r.DistanceKm ?? double.MaxValue),
            "newest" => items.OrderBy(r => r.IsDemoted ? 1 : 0).ThenByDescending(r => r.CreatedAt),
            "rating_desc" => items
                .OrderBy(r => r.IsDemoted ? 1 : 0)
                .ThenByDescending(r => r.AverageRating)
                .ThenByDescending(r => r.ReviewCount)
                .ThenByDescending(r => r.CreatedAt),
            _ => items
                .OrderBy(r => r.IsDemoted ? 1 : 0)
                .ThenByDescending(r => r.RecommendationScore)
                .ThenByDescending(r => r.CreatedAt)
        };

        var list = items.ToList();
        var pagedItems = list.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize);
        return Ok(ApiResponse<object>.Ok(new { items = pagedItems, total = list.Count, q.Page, q.PageSize }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var room = await RoomsWithDetails
            .Include(r => r.Landlord)
            .Include(r => r.University)
            .SingleOrDefaultAsync(r => r.Id == id);

        var isPublic = room != null && room.Status == RoomStatus.AVAILABLE && room.ExpiresAt > DateTime.UtcNow;
        if (room == null || (!isPublic && !CanManage(room)))
        {
            return NotFound(ApiResponse<object>.Fail("Tin không tồn tại hoặc đã ngừng hiển thị."));
        }

        var reviews = room.Reviews
            .Where(v => !v.IsFlagged)
            .OrderByDescending(v => rankingService.GetReviewWeight(v.User))
            .ThenByDescending(v => v.CreatedAt)
            .ToList();

        var landlordReviews = await db.Reviews
            .AsNoTracking()
            .Include(v => v.User)
            .Include(v => v.Room)
            .Where(v => !v.IsFlagged && v.Room.LandlordId == room.LandlordId)
            .ToListAsync();

        var demotion = rankingService.EvaluateRoomDemotion(room, landlordReviews);
        var weightedAvgRating = rankingService.CalculateWeightedRating(reviews);
        var recommendationScore = rankingService.CalculateRecommendationScore(reviews);
        double? distanceToUniversityKm = room.University == null
            ? null
            : haversine.CalculateDistanceKm(room.Latitude, room.Longitude, room.University.Latitude, room.University.Longitude);

        return Ok(ApiResponse<object>.Ok(new
        {
            room.Id,
            room.Title,
            room.Description,
            room.Address,
            room.Latitude,
            room.Longitude,
            room.BasePrice,
            room.ElectricPrice,
            room.WaterPrice,
            room.InternetPrice,
            room.OtherFee,
            room.Area,
            room.MaxPeople,
            room.Amenities,
            room.Status,
            room.ExpiresAt,
            room.CreatedAt,
            room.UniversityId,
            isDemoted = demotion.IsDemoted,
            demotionReason = demotion.Reason,
            images = room.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url),
            landlord = new
            {
                room.Landlord.Id,
                room.Landlord.FullName,
                room.Landlord.PhoneNumber,
                room.Landlord.IsEmailVerified
            },
            university = room.University == null
                ? null
                : new
                {
                    room.University.Id,
                    room.University.Name,
                    room.University.ShortName,
                    room.University.Latitude,
                    room.University.Longitude
                },
            distanceToUniversityKm,
            averageRating = weightedAvgRating,
            reviewCount = reviews.Count,
            recommendationScore,
            isLowRated = demotion.IsDemoted,
            reviews = reviews.Select(v => new
            {
                v.Id,
                v.Rating,
                v.Comment,
                v.CreatedAt,
                weight = rankingService.GetReviewWeight(v.User),
                user = new
                {
                    v.User.FullName,
                    isEduVerified = v.User.IsEduVerified || Models.User.IsEduDomain(v.User.Email),
                    v.User.IsEmailVerified,
                    isEduDomain = Models.User.IsEduDomain(v.User.Email)
                }
            })
        }));
    }

    [HttpGet("{id:guid}/cost-estimate")]
    public async Task<IActionResult> Estimate(
        Guid id,
        [FromQuery, Range(0, 10000)] decimal expectedKwh = 80,
        [FromQuery, Range(0, 1000)] decimal expectedM3 = 4)
    {
        var room = await db.Rooms.FindAsync(id);
        if (room == null || room.Status != RoomStatus.AVAILABLE || room.ExpiresAt <= DateTime.UtcNow)
        {
            return NotFound();
        }

        var total = costs.CalculateMonthlyCost(
            room.BasePrice,
            room.ElectricPrice,
            room.WaterPrice,
            room.InternetPrice,
            room.OtherFee,
            expectedKwh,
            expectedM3);

        return Ok(ApiResponse<object>.Ok(new
        {
            electricCost = room.ElectricPrice * expectedKwh,
            waterCost = room.WaterPrice * expectedM3,
            totalEstimatedMonthlyCost = total
        }));
    }

    [HttpGet("mine")]
    [Authorize(Roles = "LANDLORD,ADMIN")]
    public async Task<IActionResult> Mine()
    {
        var rooms = await RoomsWithDetails
            .AsNoTracking()
            .Where(r => r.LandlordId == User.UserId())
            .ToListAsync();

        var items = rooms
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => Mapping.Card(r, null, rankingService));

        return Ok(ApiResponse<object>.Ok(items));
    }

    [HttpPost]
    [Authorize(Roles = "LANDLORD,ADMIN")]
    public async Task<IActionResult> Create(CreateRoomDto dto)
    {
        var account = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == User.UserId());
        if (account == null)
        {
            return Unauthorized();
        }

        if (!account.IsEmailVerified)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                ApiResponse<object>.Fail("Vui lòng xác minh mã email trước khi đăng bài."));
        }

        var error = await ValidateAsync(dto);
        if (error != null)
        {
            return BadRequest(ApiResponse<object>.Fail(error));
        }

        var room = new Room { LandlordId = User.UserId() };
        ApplyRoomData(room, dto);
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        return Created($"/api/rooms/{room.Id}", ApiResponse<object>.Ok(new { room.Id }));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "LANDLORD,ADMIN")]
    public async Task<IActionResult> Update(Guid id, CreateRoomDto dto)
    {
        var room = await RoomsWithDetails.SingleOrDefaultAsync(r => r.Id == id);
        if (room == null)
        {
            return NotFound();
        }

        if (!CanManage(room))
        {
            return Forbid();
        }

        var error = await ValidateAsync(dto);
        if (error != null)
        {
            return BadRequest(ApiResponse<object>.Fail(error));
        }

        db.RoomImages.RemoveRange(room.Images);
        ApplyRoomData(room, dto);
        db.RoomImages.AddRange(room.Images);
        room.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { room.Id }));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "LANDLORD,ADMIN")]
    public async Task<IActionResult> Status(Guid id, RoomStatusUpdate dto)
    {
        var room = await db.Rooms.FindAsync(id);
        if (room == null)
        {
            return NotFound();
        }

        if (!CanManage(room))
        {
            return Forbid();
        }

        if (!Enum.IsDefined(dto.Status))
        {
            return BadRequest();
        }

        room.Status = dto.Status;
        room.UpdatedAt = DateTime.UtcNow;
        if (dto.Status == RoomStatus.AVAILABLE && (dto.Renew || room.ExpiresAt <= DateTime.UtcNow))
        {
            room.ExpiresAt = DateTime.UtcNow.AddDays(30);
        }

        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { room.Id, room.Status, room.ExpiresAt }));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "LANDLORD,ADMIN")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var room = await db.Rooms.FindAsync(id);
        if (room == null)
        {
            return NotFound();
        }

        if (!CanManage(room))
        {
            return Forbid();
        }

        room.Status = RoomStatus.HIDDEN;
        room.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { room.Id }));
    }

    private bool CanManage(Room room)
    {
        return User.Identity?.IsAuthenticated == true &&
               (User.IsInRole("ADMIN") || room.LandlordId == User.UserId());
    }

    private async Task<string?> ValidateAsync(CreateRoomDto dto)
    {
        if (dto.UniversityId.HasValue && !await db.Universities.AnyAsync(u => u.Id == dto.UniversityId))
        {
            return "Trường đại học không hợp lệ.";
        }

        var hasInvalidImage = dto.ImageUrls.Count is < 1 or > 10 ||
            dto.ImageUrls.Any(url => !UploadStorage.IsPublicImageUrl(url) &&
                (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https"));

        if (hasInvalidImage)
        {
            return "Cần 1–10 ảnh đã tải lên hoặc đường dẫn HTTPS.";
        }

        if (string.IsNullOrWhiteSpace(dto.Title) ||
            string.IsNullOrWhiteSpace(dto.Description) ||
            string.IsNullOrWhiteSpace(dto.Address))
        {
            return "Nhập đầy đủ tiêu đề, mô tả và địa chỉ.";
        }

        if (dto.Amenities.Count > 20 || dto.Amenities.Any(a => a.Length > 50))
        {
            return "Danh sách tiện ích không hợp lệ.";
        }

        return null;
    }

    private static void ApplyRoomData(Room room, CreateRoomDto dto)
    {
        room.UniversityId = dto.UniversityId;
        room.Title = dto.Title.Trim();
        room.Description = dto.Description.Trim();
        room.Address = dto.Address.Trim();
        room.Latitude = dto.Latitude;
        room.Longitude = dto.Longitude;
        room.BasePrice = dto.BasePrice;
        room.ElectricPrice = dto.ElectricPrice;
        room.WaterPrice = dto.WaterPrice;
        room.InternetPrice = dto.InternetPrice;
        room.OtherFee = dto.OtherFee;
        room.Area = dto.Area;
        room.MaxPeople = dto.MaxPeople;
        room.Amenities = dto.Amenities.Distinct().ToList();
        room.Images = dto.ImageUrls
            .Select((url, index) => new RoomImage { Url = url, IsCover = index == 0, DisplayOrder = index })
            .ToList();
    }
}

public record RoomStatusUpdate(RoomStatus Status, bool Renew = false);
