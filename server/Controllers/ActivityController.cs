using System.ComponentModel.DataAnnotations;
using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.Models;
using CampusRent.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CampusRent.Server.Controllers;
[ApiController, Route("api"), Authorize]
public class ActivityController(CampusRentDbContext db) : ControllerBase
{
    [HttpGet("favorites")]
    public async Task<IActionResult> Favorites()
    {
        var favorites = await db.Favorites.Where(f => f.UserId == User.UserId()).Include(f => f.Room).ThenInclude(r => r.Images).Include(f => f.Room).ThenInclude(r => r.Reviews).ToListAsync();
        return Ok(ApiResponse<object>.Ok(favorites.Where(f => f.Room.Status == RoomStatus.AVAILABLE && f.Room.ExpiresAt > DateTime.UtcNow).Select(f => Mapping.Card(f.Room))));
    }
    [HttpPut("favorites/{roomId:guid}")]
    public async Task<IActionResult> Save(Guid roomId)
    {
        if (!await db.Rooms.AnyAsync(r => r.Id == roomId && r.Status == RoomStatus.AVAILABLE && r.ExpiresAt > DateTime.UtcNow)) return NotFound();
        var uid = User.UserId();
        if (!await db.Favorites.AnyAsync(f => f.UserId == uid && f.RoomId == roomId))
        {
            db.Favorites.Add(new() { UserId = uid, RoomId = roomId });
            try { await db.SaveChangesAsync(); } catch (DbUpdateException) { db.ChangeTracker.Clear(); if (!await db.Favorites.AnyAsync(f => f.UserId == uid && f.RoomId == roomId)) throw; }
        }
        return Ok(ApiResponse<object>.Ok(new { roomId }));
    }
    [HttpDelete("favorites/{roomId:guid}")]
    public async Task<IActionResult> Unsave(Guid roomId)
    {
        var f = await db.Favorites.FindAsync(User.UserId(), roomId);
        if (f != null) { db.Favorites.Remove(f); await db.SaveChangesAsync(); }
        return NoContent();
    }
    [HttpGet("bookings")]
    public async Task<IActionResult> Bookings()
    {
        var uid = User.UserId();
        var bookings = await db.Bookings.Include(b => b.Room).Include(b => b.Student).Where(b => b.StudentId == uid || b.Room.LandlordId == uid).ToListAsync();
        return Ok(ApiResponse<object>.Ok(bookings.OrderByDescending(b => b.CreatedAt).Select(b => new { b.Id, b.RoomId, roomTitle = b.Room.Title, b.Room.Address, b.VisitAt, b.Note, b.Status, b.StudentId, studentName = b.Student.FullName, phoneNumber = b.Student.PhoneNumber, isLandlord = b.Room.LandlordId == uid })));
    }
    [HttpPost("rooms/{roomId:guid}/bookings")]
    public async Task<IActionResult> Book(Guid roomId, CreateBooking dto)
    {
        var room = await db.Rooms.FindAsync(roomId);
        if (room == null || room.Status != RoomStatus.AVAILABLE || room.ExpiresAt <= DateTime.UtcNow) return NotFound();
        if (room.LandlordId == User.UserId()) return BadRequest(ApiResponse<object>.Fail("Không thể đặt lịch với chính mình."));
        var visit = dto.VisitAt.UtcDateTime;
        if (visit < DateTime.UtcNow.AddHours(1) || visit > DateTime.UtcNow.AddDays(30)) return BadRequest(ApiResponse<object>.Fail("Chọn lịch sau ít nhất 1 giờ và trong 30 ngày tới."));
        if (await db.Bookings.AnyAsync(b => b.RoomId == roomId && b.StudentId == User.UserId() && b.Status != "CANCELLED" && b.VisitAt > DateTime.UtcNow))
            return Conflict(ApiResponse<object>.Fail("Bạn đã có lịch xem phòng này. Hãy hủy lịch cũ trước."));
        var b = new Booking { RoomId = roomId, StudentId = User.UserId(), VisitAt = visit, Note = dto.Note.Trim() };
        db.Bookings.Add(b);
        try { await db.SaveChangesAsync(); } catch (DbUpdateException) { return Conflict(ApiResponse<object>.Fail("Lịch hẹn này đã tồn tại.")); }
        return Ok(ApiResponse<object>.Ok(new { b.Id }, "Đã gửi yêu cầu xem phòng. Chủ trọ sẽ xác nhận lịch."));
    }
    [HttpPatch("bookings/{id:guid}")]
    public async Task<IActionResult> UpdateBooking(Guid id, BookingStatus dto)
    {
        var b = await db.Bookings.Include(b => b.Room).SingleOrDefaultAsync(b => b.Id == id);
        if (b == null) return NotFound();
        var owner = b.Room.LandlordId == User.UserId();
        if (!owner && b.StudentId != User.UserId()) return Forbid();
        if (dto.Status is not ("CONFIRMED" or "CANCELLED") || dto.Status == "CONFIRMED" && !owner || b.Status == "CANCELLED" || b.VisitAt < DateTime.UtcNow)
            return BadRequest(ApiResponse<object>.Fail("Không thể thay đổi trạng thái lịch hẹn này."));
        b.Status = dto.Status; await db.SaveChangesAsync(); return Ok(ApiResponse<object>.Ok(new { b.Id, b.Status }));
    }
}
public record CreateBooking(DateTimeOffset VisitAt, [MaxLength(1000)] string Note = "");
public record BookingStatus(string Status);


