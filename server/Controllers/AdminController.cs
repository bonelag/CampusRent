using System.Text;
using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Admin;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.DTOs.LandlordRequests;
using CampusRent.Server.DTOs.Reports;
using CampusRent.Server.DTOs.Universities;
using CampusRent.Server.Models;
using CampusRent.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusRent.Server.Controllers;

[ApiController, Route("api/admin"), Authorize(Roles = "ADMIN")]
public class AdminController(CampusRentDbContext db, IEmailService emailService) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var now = DateTime.UtcNow;
        var activityStart = now.Date.AddDays(-6);

        var users = await db.Users.AsNoTracking()
            .Select(u => new { u.Role, u.IsEmailVerified, u.CreatedAt })
            .ToListAsync();
        var rooms = await db.Rooms.AsNoTracking()
            .Select(r => new { r.Status, r.ExpiresAt, r.CreatedAt })
            .ToListAsync();
        var bookings = await db.Bookings.AsNoTracking()
            .Select(b => new { b.Status, b.CreatedAt })
            .ToListAsync();
        var reports = await db.Reports.AsNoTracking()
            .Select(r => new { r.Status, r.CreatedAt })
            .ToListAsync();
        var reviewFlags = await db.Reviews.AsNoTracking().Select(item => item.IsFlagged).ToListAsync();
        var flaggedReviews = reviewFlags.Count(x => x);
        var reviews = reviewFlags.Count;
        var favorites = await db.Favorites.CountAsync();
        var landlordRequests = await db.LandlordRequests.AsNoTracking().Select(x => x.Status).ToListAsync();
        var universities = await db.Universities.CountAsync();
        var smtp = await emailService.GetEffectiveSettingsAsync();

        var activity = Enumerable.Range(0, 7).Select(offset =>
        {
            var day = activityStart.AddDays(offset);
            var dayEnd = day.AddDays(1);
            return new
            {
                date = day.ToString("dd/MM"),
                users = users.Count(x => x.CreatedAt >= day && x.CreatedAt < dayEnd),
                rooms = rooms.Count(x => x.CreatedAt >= day && x.CreatedAt < dayEnd),
                bookings = bookings.Count(x => x.CreatedAt >= day && x.CreatedAt < dayEnd),
                reports = reports.Count(x => x.CreatedAt >= day && x.CreatedAt < dayEnd)
            };
        });

        var pendingReports = reports.Count(x => x.Status == ReportStatus.PENDING);
        var pendingLandlordRequests = landlordRequests.Count(x => x == LandlordRequestStatus.PENDING);
        var pendingBookings = bookings.Count(x => x.Status == "PENDING");

        return Ok(ApiResponse<object>.Ok(new
        {
            users = users.Count,
            verifiedEmails = users.Count(x => x.IsEmailVerified),
            smtpConfigured = smtp.Enabled && !string.IsNullOrWhiteSpace(smtp.Username) && !string.IsNullOrWhiteSpace(smtp.Password),
            students = users.Count(x => x.Role == Role.STUDENT),
            landlords = users.Count(x => x.Role == Role.LANDLORD),
            administrators = users.Count(x => x.Role == Role.ADMIN),
            rooms = rooms.Count,
            availableRooms = rooms.Count(x => x.Status == RoomStatus.AVAILABLE && x.ExpiresAt > now),
            rentedRooms = rooms.Count(x => x.Status == RoomStatus.RENTED),
            hiddenRooms = rooms.Count(x => x.Status == RoomStatus.HIDDEN || (x.Status == RoomStatus.AVAILABLE && x.ExpiresAt <= now)),
            bookings = bookings.Count,
            pendingBookings,
            confirmedBookings = bookings.Count(x => x.Status == "CONFIRMED"),
            favorites,
            reviews,
            flaggedReviews,
            reports = reports.Count,
            pendingReports,
            landlordRequests = landlordRequests.Count,
            pendingLandlordRequests,
            universities,
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
            user.IsEmailVerified,
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

    [HttpPatch("users/{id:guid}/verification")]
    public async Task<IActionResult> UpdateUserVerification(Guid id, AdminUserVerificationDto dto)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound(ApiResponse<object>.Fail("Không tìm thấy người dùng."));
        user.IsEmailVerified = dto.IsEmailVerified;
        user.IsEduVerified = dto.IsEduVerified;
        if (dto.IsEmailVerified)
        {
            user.EmailVerificationCodeHash = null;
            user.EmailVerificationExpiresAt = null;
            user.EmailVerificationAttempts = 0;
        }
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new
        {
            user.Id,
            user.IsEmailVerified,
            user.IsEduVerified,
            user.UpdatedAt
        }, "Đã cập nhật trạng thái xác minh tài khoản."));
    }

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

    [HttpGet("smtp")]
    public async Task<IActionResult> GetSmtpSettings(CancellationToken ct)
    {
        var s = await emailService.GetEffectiveSettingsAsync(ct);
        return Ok(ApiResponse<object>.Ok(SmtpResponse(s)));
    }

    [HttpPut("smtp")]
    public async Task<IActionResult> UpdateSmtpSettings(SmtpSettingsUpdateDto dto, CancellationToken ct)
    {
        var existing = await db.SmtpSettings.SingleOrDefaultAsync(x => x.Id == 1, ct);
        if (existing == null)
        {
            existing = new SmtpSetting { Id = 1 };
            db.SmtpSettings.Add(existing);
        }

        existing.Enabled = dto.Enabled;
        existing.Host = dto.Host.Trim();
        existing.Port = dto.Port;
        existing.UseSsl = dto.UseSsl;
        existing.Username = dto.Username.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Password))
            existing.Password = emailService.ProtectSecret(dto.Password.Trim());
        else if (string.IsNullOrWhiteSpace(existing.Password))
        {
            var effective = await emailService.GetEffectiveSettingsAsync(ct);
            if (!string.IsNullOrWhiteSpace(effective.Password))
                existing.Password = emailService.ProtectSecret(emailService.UnprotectSecret(effective.Password));
        }

        existing.FromEmail = string.IsNullOrWhiteSpace(dto.FromEmail) ? existing.Username : dto.FromEmail.Trim();
        existing.FromName = string.IsNullOrWhiteSpace(dto.FromName) ? "CampusRent" : dto.FromName.Trim();
        existing.UpdatedAt = DateTime.UtcNow;

        if (existing.Enabled && (string.IsNullOrWhiteSpace(existing.Host) || string.IsNullOrWhiteSpace(existing.Username) || string.IsNullOrWhiteSpace(existing.Password)))
            return BadRequest(ApiResponse<object>.Fail("Cần nhập đầy đủ SMTP Host, Tài khoản Gmail và Mật khẩu ứng dụng khi bật SMTP."));

        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(SmtpResponse(existing), "Đã lưu cấu hình máy chủ SMTP."));
    }

    [HttpPost("smtp/test")]
    public async Task<IActionResult> SendSmtpTestEmail(AdminTestEmailDto? dto, CancellationToken ct)
    {
        var target = dto?.ToEmail?.Trim();
        if (string.IsNullOrWhiteSpace(target))
        {
            var admin = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == User.UserId(), ct);
            target = admin?.Email;
        }
        if (string.IsNullOrWhiteSpace(target) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(target))
            return BadRequest(ApiResponse<object>.Fail("Địa chỉ email nhận thử không hợp lệ."));

        var result = await emailService.SendTestEmailAsync(target, null, ct);
        if (!result.Sent)
            return BadRequest(ApiResponse<object>.Fail(result.Error ?? "Gửi email kiểm tra thất bại."));

        return Ok(ApiResponse<object>.Ok(new { toEmail = target }, $"Đã gửi email kiểm tra thành công tới {target}."));
    }

    private object SmtpResponse(SmtpSetting s) => new
    {
        s.Enabled,
        s.Host,
        s.Port,
        s.UseSsl,
        s.Username,
        hasPassword = !string.IsNullOrWhiteSpace(emailService.UnprotectSecret(s.Password)),
        s.FromEmail,
        s.FromName,
        s.UpdatedAt
    };
}
