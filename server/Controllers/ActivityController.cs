using System.ComponentModel.DataAnnotations;
using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.Models;
using CampusRent.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusRent.Server.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class ActivityController(CampusRentDbContext db, IReviewRankingService rankingService) : ControllerBase
{
    [HttpGet("favorites")]
    public async Task<IActionResult> Favorites()
    {
        var userId = User.UserId();
        var favorites = await db.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .Include(f => f.Room).ThenInclude(r => r.Images)
            .Include(f => f.Room).ThenInclude(r => r.Reviews).ThenInclude(v => v.User)
            .AsSplitQuery()
            .ToListAsync();

        var visibleRooms = favorites
            .Where(f => f.Room.Status == RoomStatus.AVAILABLE && f.Room.ExpiresAt > DateTime.UtcNow)
            .Select(f => Mapping.Card(f.Room, null, rankingService));

        return Ok(ApiResponse<object>.Ok(visibleRooms));
    }

    [HttpPut("favorites/{roomId:guid}")]
    public async Task<IActionResult> Save(Guid roomId)
    {
        var roomAvailable = await db.Rooms.AnyAsync(
            r => r.Id == roomId && r.Status == RoomStatus.AVAILABLE && r.ExpiresAt > DateTime.UtcNow);
        if (!roomAvailable)
        {
            return NotFound();
        }

        var userId = User.UserId();
        var alreadySaved = await db.Favorites.AnyAsync(f => f.UserId == userId && f.RoomId == roomId);
        if (!alreadySaved)
        {
            db.Favorites.Add(new Favorite { UserId = userId, RoomId = roomId });
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear();
                if (!await db.Favorites.AnyAsync(f => f.UserId == userId && f.RoomId == roomId))
                {
                    throw;
                }
            }
        }

        return Ok(ApiResponse<object>.Ok(new { roomId }));
    }

    [HttpDelete("favorites/{roomId:guid}")]
    public async Task<IActionResult> Unsave(Guid roomId)
    {
        var favorite = await db.Favorites.FindAsync(User.UserId(), roomId);
        if (favorite != null)
        {
            db.Favorites.Remove(favorite);
            await db.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpGet("bookings")]
    public async Task<IActionResult> Bookings()
    {
        var userId = User.UserId();
        var bookings = await db.Bookings
            .AsNoTracking()
            .Include(b => b.Room)
            .Include(b => b.Student)
            .Where(b => b.StudentId == userId || b.Room.LandlordId == userId)
            .ToListAsync();

        var items = bookings
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                b.Id,
                b.RoomId,
                roomTitle = b.Room.Title,
                b.Room.Address,
                roomStatus = b.Room.Status,
                roomExpiresAt = b.Room.ExpiresAt,
                b.VisitAt,
                b.Note,
                b.Status,
                b.ConfirmedAt,
                b.ReminderSentAt,
                b.CustomerNotClosedAt,
                b.AutoClosedAt,
                b.StudentId,
                studentName = b.Student.FullName,
                phoneNumber = b.Student.PhoneNumber,
                isLandlord = b.Room.LandlordId == userId
            });

        return Ok(ApiResponse<object>.Ok(items));
    }

    [HttpPost("rooms/{roomId:guid}/bookings")]
    public async Task<IActionResult> Book(Guid roomId, CreateBooking dto)
    {
        var room = await db.Rooms.FindAsync(roomId);
        if (room == null || room.Status != RoomStatus.AVAILABLE || room.ExpiresAt <= DateTime.UtcNow)
        {
            return NotFound();
        }

        var userId = User.UserId();
        if (room.LandlordId == userId)
        {
            return BadRequest(ApiResponse<object>.Fail("Không thể đặt lịch với chính mình."));
        }

        var now = DateTime.UtcNow;
        var visitAt = dto.VisitAt.UtcDateTime;
        if (visitAt < now.AddHours(1) || visitAt > now.AddDays(30))
        {
            return BadRequest(ApiResponse<object>.Fail("Chọn lịch sau ít nhất 1 giờ và trong 30 ngày tới."));
        }

        var pastBookings = await db.Bookings
            .Where(b => b.RoomId == roomId
                && b.StudentId == userId
                && (b.Status == "PENDING" || b.Status == "CONFIRMED")
                && b.VisitAt <= now)
            .ToListAsync();

        foreach (var oldBooking in pastBookings)
        {
            oldBooking.Status = "COMPLETED";
        }
        await db.SaveChangesAsync();

        var hasActiveBooking = await db.Bookings.AnyAsync(
            b => b.RoomId == roomId && b.StudentId == userId && b.Status != "CANCELLED" && b.VisitAt > now);
        if (hasActiveBooking)
        {
            return Conflict(ApiResponse<object>.Fail("Bạn đã có lịch xem phòng này. Hãy hủy lịch cũ trước."));
        }

        var booking = new Booking
        {
            RoomId = roomId,
            StudentId = userId,
            VisitAt = visitAt,
            Note = dto.Note.Trim()
        };

        db.Bookings.Add(booking);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(ApiResponse<object>.Fail("Lịch hẹn này đã tồn tại."));
        }

        return Ok(ApiResponse<object>.Ok(new { booking.Id }, "Đã gửi yêu cầu xem phòng. Chủ trọ sẽ xác nhận lịch."));
    }

    [HttpPatch("bookings/{id:guid}")]
    public async Task<IActionResult> UpdateBooking(Guid id, BookingStatus dto)
    {
        var booking = await db.Bookings
            .Include(b => b.Room)
            .SingleOrDefaultAsync(b => b.Id == id);
        if (booking == null)
        {
            return NotFound();
        }

        var isLandlord = booking.Room.LandlordId == User.UserId();
        if (!isLandlord && booking.StudentId != User.UserId())
        {
            return Forbid();
        }

        var isValidNextStatus = dto.Status is "CONFIRMED" or "CANCELLED";
        var isInvalidRoleAction = dto.Status == "CONFIRMED" && !isLandlord;
        var isAlreadyClosed = booking.Status is "CANCELLED" or "COMPLETED";
        var isExpiredVisit = booking.VisitAt < DateTime.UtcNow;

        if (!isValidNextStatus || isInvalidRoleAction || isAlreadyClosed || isExpiredVisit)
        {
            return BadRequest(ApiResponse<object>.Fail("Không thể thay đổi trạng thái lịch hẹn này."));
        }

        booking.Status = dto.Status;
        if (dto.Status == "CONFIRMED" && booking.ConfirmedAt == null)
        {
            booking.ConfirmedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { booking.Id, booking.Status, booking.ConfirmedAt }));
    }

    [HttpPatch("bookings/{id:guid}/not-closed")]
    public async Task<IActionResult> MarkCustomerNotClosed(Guid id, BookingNotClosedDto dto)
    {
        var booking = await db.Bookings
            .Include(x => x.Room)
            .SingleOrDefaultAsync(x => x.Id == id);
        if (booking == null)
        {
            return NotFound();
        }

        if (booking.Room.LandlordId != User.UserId())
        {
            return Forbid();
        }

        if (booking.Status == "CANCELLED" || (booking.Status != "CONFIRMED" && booking.ConfirmedAt == null))
        {
            return BadRequest(ApiResponse<object>.Fail("Chỉ áp dụng cho lịch hẹn đã được chủ trọ xác nhận."));
        }

        var now = DateTime.UtcNow;
        if (dto.NotClosed)
        {
            booking.CustomerNotClosedAt = now;
            if (booking.AutoClosedAt != null)
            {
                booking.AutoClosedAt = null;
                if (booking.Room.Status == RoomStatus.HIDDEN && booking.Room.ExpiresAt > now)
                {
                    booking.Room.Status = RoomStatus.AVAILABLE;
                    booking.Room.UpdatedAt = now;
                }

                if (booking.Status == "COMPLETED" && booking.VisitAt.AddDays(3) > now)
                {
                    booking.Status = "CONFIRMED";
                }
            }
        }
        else
        {
            booking.CustomerNotClosedAt = null;
        }

        await db.SaveChangesAsync();
        var message = dto.NotClosed
            ? "Đã xác nhận khách chưa chốt — phòng tiếp tục hiển thị."
            : "Đã hủy đánh dấu khách chưa chốt.";

        return Ok(ApiResponse<object>.Ok(new
        {
            booking.Id,
            booking.Status,
            booking.CustomerNotClosedAt,
            booking.AutoClosedAt,
            roomStatus = booking.Room.Status
        }, message));
    }
}

public record CreateBooking(DateTimeOffset VisitAt, [MaxLength(1000)] string Note = "");
public record BookingStatus(string Status);
public record BookingNotClosedDto(bool NotClosed = true);
