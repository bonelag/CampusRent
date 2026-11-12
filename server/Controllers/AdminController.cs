using System.Text;
using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Admin;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.DTOs.LandlordRequests;
using CampusRent.Server.DTOs.Reports;
using CampusRent.Server.DTOs.Universities;
using CampusRent.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusRent.Server.Controllers;

[ApiController, Route("api/admin"), Authorize(Roles = "ADMIN")]
public class AdminController(CampusRentDbContext db) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var now = DateTime.UtcNow;
        var activityStart = now.Date.AddDays(-6);
        var userDates = await db.Users.AsNoTracking().Where(item => item.CreatedAt >= activityStart).Select(item => item.CreatedAt).ToListAsync();
        var roomDates = await db.Rooms.AsNoTracking().Where(item => item.CreatedAt >= activityStart).Select(item => item.CreatedAt).ToListAsync();
        var bookingDates = await db.Bookings.AsNoTracking().Where(item => item.CreatedAt >= activityStart).Select(item => item.CreatedAt).ToListAsync();
        var reportDates = await db.Reports.AsNoTracking().Where(item => item.CreatedAt >= activityStart).Select(item => item.CreatedAt).ToListAsync();
        var activity = Enumerable.Range(0, 7).Select(offset =>
        {
            var day = activityStart.AddDays(offset);
            return new
            {
                date = day.ToString("dd/MM"),
                users = userDates.Count(value => value.Date == day),
                rooms = roomDates.Count(value => value.Date == day),
                bookings = bookingDates.Count(value => value.Date == day),
                reports = reportDates.Count(value => value.Date == day)
            };
        });

        var pendingReports = await db.Reports.CountAsync(item => item.Status == ReportStatus.PENDING);
        var flaggedReviews = await db.Reviews.CountAsync(item => item.IsFlagged);
        var pendingLandlordRequests = await db.LandlordRequests.CountAsync(item => item.Status == LandlordRequestStatus.PENDING);
        var pendingBookings = await db.Bookings.CountAsync(item => item.Status == "PENDING");
        return Ok(ApiResponse<object>.Ok(new
        {
            users = await db.Users.CountAsync(),
            students = await db.Users.CountAsync(item => item.Role == Role.STUDENT),
            landlords = await db.Users.CountAsync(item => item.Role == Role.LANDLORD),
            administrators = await db.Users.CountAsync(item => item.Role == Role.ADMIN),
            rooms = await db.Rooms.CountAsync(),
            availableRooms = await db.Rooms.CountAsync(item => item.Status == RoomStatus.AVAILABLE && item.ExpiresAt > now),
            rentedRooms = await db.Rooms.CountAsync(item => item.Status == RoomStatus.RENTED),
            hiddenRooms = await db.Rooms.CountAsync(item => item.Status == RoomStatus.HIDDEN || item.Status == RoomStatus.AVAILABLE && item.ExpiresAt <= now),
            bookings = await db.Bookings.CountAsync(),
            pendingBookings,
            confirmedBookings = await db.Bookings.CountAsync(item => item.Status == "CONFIRMED"),
            favorites = await db.Favorites.CountAsync(),
            reviews = await db.Reviews.CountAsync(),
            flaggedReviews,
            reports = await db.Reports.CountAsync(),
            pendingReports,
            landlordRequests = await db.LandlordRequests.CountAsync(),
            pendingLandlordRequests,
            universities = await db.Universities.CountAsync(),
            moderationBacklog = pendingReports + flaggedReviews + pendingLandlordRequests + pendingBookings,
            activity
        }));
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery] string? q)
    {
        var query = db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(user => user.FullName.ToLower().Contains(term) || user.Email.ToLower().Contains(term));
        }

        return Ok(ApiResponse<object>.Ok(await query.OrderByDescending(user => user.CreatedAt).Select(user => new
        {
            user.Id,
            user.Email,
            user.FullName,
            user.Role,
            user.IsEduVerified,
            user.CreatedAt,
            user.UpdatedAt,
            RoomsCount = user.Rooms.Count,
            ReviewsCount = user.Reviews.Count,
            ReportsCount = user.Reports.Count,
            LandlordRequestsCount = user.LandlordRequests.Count
        }).ToListAsync()));
    }

    [HttpPut("users/{id:guid}")]
    public IActionResult BlockProfileEditing(Guid id) => StatusCode(StatusCodes.Status403Forbidden,
        ApiResponse<object>.Fail("Quản trị viên không được sửa thông tin cá nhân hoặc vai trò của người dùng; chỉ có thể đặt lại mật khẩu."));

    [HttpPut("users/{id:guid}/password")]
    public async Task<IActionResult> ResetPassword(Guid id, AdminResetPasswordDto dto)
    {
        if (Encoding.UTF8.GetByteCount(dto.Password) > 72)
            return BadRequest(ApiResponse<object>.Fail("Mật khẩu không được vượt quá 72 byte."));
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy người dùng."));
        user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { user.Id, user.UpdatedAt }, "Đã đặt lại mật khẩu và vô hiệu hóa các phiên cũ."));
    }

    [HttpGet("rooms")]
    public async Task<IActionResult> Rooms() => Ok(ApiResponse<object>.Ok(await db.Rooms.AsNoTracking()
        .OrderByDescending(room => room.CreatedAt)
        .Select(room => new
        {
            room.Id,
            room.Title,
            room.Address,
            room.BasePrice,
            room.Area,
            room.Status,
            room.ExpiresAt,
            room.CreatedAt,
            room.UpdatedAt,
            landlordName = room.Landlord.FullName,
            landlordEmail = room.Landlord.Email,
            universityName = room.University == null ? null : room.University.Name,
            coverImage = room.Images.OrderBy(image => image.DisplayOrder).Select(image => image.Url).FirstOrDefault(),
            reviewCount = room.Reviews.Count,
            reportCount = room.Reports.Count,
            bookingCount = db.Bookings.Count(booking => booking.RoomId == room.Id),
            favoriteCount = db.Favorites.Count(favorite => favorite.RoomId == room.Id)
        }).ToListAsync()));

    [HttpPatch("rooms/{id:guid}/status")]
    public async Task<IActionResult> UpdateRoomStatus(Guid id, AdminRoomStatusDto dto)
    {
        if (!Enum.IsDefined(dto.Status)) return BadRequest(ApiResponse<object>.Fail("Trạng thái phòng không hợp lệ."));
        var room = await db.Rooms.FindAsync(id);
        if (room == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy phòng."));
        room.Status = dto.Status;
        room.UpdatedAt = DateTime.UtcNow;
        if (dto.Renew) room.ExpiresAt = DateTime.UtcNow.AddDays(30);
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { room.Id, room.Status, room.ExpiresAt }));
    }

    [HttpDelete("rooms/{id:guid}")]
    public async Task<IActionResult> ArchiveRoom(Guid id)
    {
        var room = await db.Rooms.FindAsync(id);
        if (room == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy phòng."));
        room.Status = RoomStatus.HIDDEN;
        room.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { room.Id, room.Status }));
    }

    [HttpGet("bookings")]
    public async Task<IActionResult> Bookings() => Ok(ApiResponse<object>.Ok(await db.Bookings.AsNoTracking()
        .OrderByDescending(booking => booking.CreatedAt)
        .Select(booking => new
        {
            booking.Id,
            booking.RoomId,
            roomTitle = booking.Room.Title,
            booking.StudentId,
            studentName = booking.Student.FullName,
            studentEmail = booking.Student.Email,
            landlordName = booking.Room.Landlord.FullName,
            booking.VisitAt,
            booking.Note,
            booking.Status,
            booking.CreatedAt
        }).ToListAsync()));

    [HttpPatch("bookings/{id:guid}")]
    public async Task<IActionResult> UpdateBooking(Guid id, AdminBookingStatusDto dto)
    {
        var status = dto.Status.Trim().ToUpperInvariant();
        if (status is not ("PENDING" or "CONFIRMED" or "CANCELLED" or "COMPLETED"))
            return BadRequest(ApiResponse<object>.Fail("Trạng thái lịch hẹn không hợp lệ."));
        var booking = await db.Bookings.FindAsync(id);
        if (booking == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy lịch hẹn."));
        booking.Status = status;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { booking.Id, booking.Status }));
    }

    [HttpDelete("bookings/{id:guid}")]
    public async Task<IActionResult> DeleteBooking(Guid id)
    {
        var booking = await db.Bookings.FindAsync(id);
        if (booking == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy lịch hẹn."));
        db.Bookings.Remove(booking);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("favorites")]
    public async Task<IActionResult> Favorites() => Ok(ApiResponse<object>.Ok(await db.Favorites.AsNoTracking()
        .OrderBy(favorite => favorite.User.FullName).ThenBy(favorite => favorite.Room.Title)
        .Select(favorite => new
        {
            favorite.UserId,
            userName = favorite.User.FullName,
            userEmail = favorite.User.Email,
            favorite.RoomId,
            roomTitle = favorite.Room.Title,
            roomStatus = favorite.Room.Status,
            coverImage = favorite.Room.Images.OrderBy(image => image.DisplayOrder).Select(image => image.Url).FirstOrDefault()
        }).ToListAsync()));

    [HttpDelete("favorites/{userId:guid}/{roomId:guid}")]
    public async Task<IActionResult> DeleteFavorite(Guid userId, Guid roomId)
    {
        var favorite = await db.Favorites.FindAsync(userId, roomId);
        if (favorite == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy lượt lưu phòng."));
        db.Favorites.Remove(favorite);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("reviews")]
    public async Task<IActionResult> Reviews() => Ok(ApiResponse<object>.Ok(await db.Reviews.AsNoTracking()
        .OrderByDescending(review => review.CreatedAt)
        .Select(review => new
        {
            review.Id,
            review.RoomId,
            roomTitle = review.Room.Title,
            review.UserId,
            userName = review.User.FullName,
            userEmail = review.User.Email,
            review.Rating,
            review.Comment,
            review.IsFlagged,
            review.FlagReason,
            review.CreatedAt,
            reportCount = review.Reports.Count
        }).ToListAsync()));

    [HttpPatch("reviews/{id:guid}")]
    public async Task<IActionResult> UpdateReview(Guid id, AdminReviewUpdateDto dto)
    {
        var review = await db.Reviews.FindAsync(id);
        if (review == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy đánh giá."));
        review.IsFlagged = dto.IsFlagged;
        review.FlagReason = dto.IsFlagged ? dto.FlagReason?.Trim() ?? "Quản trị viên ẩn nội dung" : null;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { review.Id, review.IsFlagged, review.FlagReason }));
    }

    [HttpPatch("reviews/{id:guid}/approve")]
    public async Task<IActionResult> ApproveReview(Guid id) => await UpdateReview(id, new AdminReviewUpdateDto(false, null));

    [HttpDelete("reviews/{id:guid}")]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        var review = await db.Reviews.FindAsync(id);
        if (review == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy đánh giá."));
        db.Reviews.Remove(review);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("reports")]
    public async Task<IActionResult> Reports() => Ok(ApiResponse<object>.Ok(await db.Reports.AsNoTracking()
        .OrderByDescending(report => report.CreatedAt)
        .Select(report => new
        {
            report.Id,
            report.RoomId,
            report.ReviewId,
            report.Reason,
            report.Description,
            report.Status,
            report.CreatedAt,
            roomTitle = report.Room == null ? null : report.Room.Title,
            reviewComment = report.Review == null ? null : report.Review.Comment,
            reporterName = report.Reporter.FullName,
            reporterEmail = report.Reporter.Email
        }).ToListAsync()));

    [HttpPatch("reports/{id:guid}")]
    public async Task<IActionResult> ResolveReport(Guid id, ResolveReportDto dto)
    {
        if (dto.Status is not (ReportStatus.RESOLVED or ReportStatus.DISMISSED) || dto.DeleteTarget && dto.Status != ReportStatus.RESOLVED)
            return BadRequest(ApiResponse<object>.Fail("Trạng thái xử lý báo cáo không hợp lệ."));
        var report = await db.Reports.Include(item => item.Room).Include(item => item.Review).SingleOrDefaultAsync(item => item.Id == id);
        if (report == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy báo cáo."));
        if (report.Status != ReportStatus.PENDING) return Conflict(ApiResponse<object>.Fail("Báo cáo đã được xử lý."));
        report.Status = dto.Status;
        if (dto.DeleteTarget)
        {
            if (report.Room != null) report.Room.Status = RoomStatus.HIDDEN;
            if (report.Review != null)
            {
                report.Review.IsFlagged = true;
                report.Review.FlagReason ??= "Bị ẩn sau khi xử lý báo cáo";
            }
        }
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { report.Id, report.Status }));
    }

    [HttpDelete("reports/{id:guid}")]
    public async Task<IActionResult> DeleteReport(Guid id)
    {
        var report = await db.Reports.FindAsync(id);
        if (report == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy báo cáo."));
        db.Reports.Remove(report);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("landlord-requests")]
    public async Task<IActionResult> LandlordRequests() => Ok(ApiResponse<object>.Ok(await db.LandlordRequests.AsNoTracking()
        .OrderByDescending(request => request.CreatedAt)
        .Select(request => new
        {
            request.Id,
            request.UserId,
            userName = request.User.FullName,
            userEmail = request.User.Email,
            request.Title,
            request.Reason,
            request.ImageUrls,
            request.PdfUrl,
            request.PdfFileName,
            request.Status,
            request.AdminNote,
            request.CreatedAt,
            request.UpdatedAt
        }).ToListAsync()));

    [HttpPatch("landlord-requests/{id:guid}/approve")]
    public async Task<IActionResult> ApproveLandlordRequest(Guid id)
    {
        var request = await db.LandlordRequests.Include(item => item.User).FirstOrDefaultAsync(item => item.Id == id);
        if (request == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy yêu cầu."));
        if (request.Status != LandlordRequestStatus.PENDING) return Conflict(ApiResponse<object>.Fail("Yêu cầu đã được xử lý trước đó."));
        request.Status = LandlordRequestStatus.APPROVED;
        request.UpdatedAt = DateTime.UtcNow;
        request.User.Role = Role.LANDLORD;
        request.User.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { request.Id, status = request.Status, userRole = request.User.Role }));
    }

    [HttpPatch("landlord-requests/{id:guid}/reject")]
    public async Task<IActionResult> RejectLandlordRequest(Guid id, RejectLandlordRequestDto? dto)
    {
        var request = await db.LandlordRequests.FindAsync(id);
        if (request == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy yêu cầu."));
        if (request.Status != LandlordRequestStatus.PENDING) return Conflict(ApiResponse<object>.Fail("Yêu cầu đã được xử lý trước đó."));
        request.Status = LandlordRequestStatus.REJECTED;
        request.AdminNote = dto?.AdminNote?.Trim();
        request.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { request.Id, status = request.Status }));
    }

    [HttpDelete("landlord-requests/{id:guid}")]
    public async Task<IActionResult> DeleteLandlordRequest(Guid id)
    {
        var request = await db.LandlordRequests.FindAsync(id);
        if (request == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy yêu cầu."));
        db.LandlordRequests.Remove(request);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("universities")]
    public async Task<IActionResult> Universities() => Ok(ApiResponse<object>.Ok(await db.Universities.AsNoTracking()
        .OrderBy(university => university.Name)
        .Select(university => new
        {
            university.Id,
            university.Name,
            university.ShortName,
            university.Address,
            university.Latitude,
            university.Longitude,
            university.CreatedAt,
            roomsCount = university.Rooms.Count
        }).ToListAsync()));

    [HttpPost("universities")]
    public async Task<IActionResult> CreateUniversity(CreateUniversityDto dto)
    {
        var shortName = dto.ShortName.Trim().ToUpperInvariant();
        if (await db.Universities.AnyAsync(item => item.ShortName == shortName))
            return Conflict(ApiResponse<object>.Fail("Mã trường đã tồn tại."));
        var university = new University
        {
            Name = dto.Name.Trim(), ShortName = shortName, Address = dto.Address.Trim(),
            Latitude = dto.Latitude, Longitude = dto.Longitude
        };
        db.Universities.Add(university);
        await db.SaveChangesAsync();
        return Created($"/api/admin/universities/{university.Id}", ApiResponse<object>.Ok(new { university.Id }));
    }

    [HttpPut("universities/{id:guid}")]
    public async Task<IActionResult> UpdateUniversity(Guid id, CreateUniversityDto dto)
    {
        var university = await db.Universities.FindAsync(id);
        if (university == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy trường đại học."));
        var shortName = dto.ShortName.Trim().ToUpperInvariant();
        if (await db.Universities.AnyAsync(item => item.Id != id && item.ShortName == shortName))
            return Conflict(ApiResponse<object>.Fail("Mã trường đã tồn tại."));
        university.Name = dto.Name.Trim();
        university.ShortName = shortName;
        university.Address = dto.Address.Trim();
        university.Latitude = dto.Latitude;
        university.Longitude = dto.Longitude;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { university.Id }));
    }

    [HttpDelete("universities/{id:guid}")]
    public async Task<IActionResult> DeleteUniversity(Guid id)
    {
        var university = await db.Universities.FindAsync(id);
        if (university == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy trường đại học."));
        db.Universities.Remove(university);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
