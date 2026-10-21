using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.DTOs.Reviews;
using CampusRent.Server.Models;
using CampusRent.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CampusRent.Server.Controllers;
[ApiController, Route("api/rooms/{roomId:guid}/reviews")]
public class ReviewsController(CampusRentDbContext db, IProfanityFilterService filter) : ControllerBase
{
    [HttpPost, Authorize]
    public async Task<IActionResult> Create(Guid roomId, CreateReviewDto dto)
    {
        var room = await db.Rooms.FindAsync(roomId);
        if (room == null || room.Status != RoomStatus.AVAILABLE || room.ExpiresAt <= DateTime.UtcNow) return NotFound();
        if (room.LandlordId == User.UserId()) return BadRequest(ApiResponse<object>.Fail("Bạn không thể đánh giá tin của mình."));
        if (await db.Reviews.AnyAsync(r => r.RoomId == roomId && r.UserId == User.UserId())) return Conflict(ApiResponse<object>.Fail("Bạn đã đánh giá phòng này."));
        var flag = filter.CheckProfanity(dto.Comment);
        var review = new Review { RoomId = roomId, UserId = User.UserId(), Rating = dto.Rating, Comment = dto.Comment.Trim(), IsFlagged = flag.IsFlagged, FlagReason = flag.MatchedWord };
        db.Reviews.Add(review);
        try { await db.SaveChangesAsync(); } catch (DbUpdateException) { return Conflict(ApiResponse<object>.Fail("Bạn đã đánh giá phòng này.")); }
        return Ok(ApiResponse<object>.Ok(new { review.Id }, flag.IsFlagged ? "Đánh giá đang chờ kiểm duyệt." : "Cảm ơn bạn đã chia sẻ trải nghiệm!"));
    }
}
