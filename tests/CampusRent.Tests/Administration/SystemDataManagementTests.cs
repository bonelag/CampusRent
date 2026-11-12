using System.Net;
using System.Net.Http.Json;
using CampusRent.Server.Data;
using CampusRent.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CampusRent.Tests;

public class SystemDataManagementTests : ApiTestBase
{
    [Fact]
    public async Task AdminCanListAndManageAllBusinessData()
    {
        var studentId = await Register("STUDENT", "admin-data-student@example.com");
        var roomId = await Room();
        Guid bookingId, reviewId, reportId, requestId, universityId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
            var university = new University
            {
                Name = "Trường quản trị dữ liệu",
                ShortName = "ADMIN-DATA",
                Address = "Hà Nội",
                Latitude = 21.02,
                Longitude = 105.8
            };
            var booking = new Booking
            {
                RoomId = roomId,
                StudentId = studentId,
                VisitAt = DateTime.UtcNow.AddDays(2),
                Note = "Admin management test"
            };
            var review = new Review
            {
                RoomId = roomId,
                UserId = studentId,
                Rating = 4,
                Comment = "Đánh giá dành cho quản trị"
            };
            var report = new Report
            {
                ReporterId = studentId,
                RoomId = roomId,
                Reason = "Báo cáo dành cho quản trị"
            };
            var request = new LandlordRequest
            {
                UserId = studentId,
                Title = "Yêu cầu đã xử lý",
                Reason = "Dùng để kiểm tra quản trị",
                Status = LandlordRequestStatus.REJECTED
            };
            db.AddRange(university, booking, review, report, request);
            db.Favorites.Add(new Favorite { UserId = studentId, RoomId = roomId });
            await db.SaveChangesAsync();
            bookingId = booking.Id;
            reviewId = review.Id;
            reportId = report.Id;
            requestId = request.Id;
            universityId = university.Id;
        }

        await Admin();
        var stats = await Data(await client.GetAsync("/api/admin/stats"));
        Assert.Equal(7, stats.GetProperty("activity").GetArrayLength());
        Assert.Contains((await Data(await client.GetAsync("/api/admin/rooms"))).EnumerateArray(), item => item.GetProperty("id").GetGuid() == roomId);
        Assert.Contains((await Data(await client.GetAsync("/api/admin/bookings"))).EnumerateArray(), item => item.GetProperty("id").GetGuid() == bookingId);
        Assert.Contains((await Data(await client.GetAsync("/api/admin/favorites"))).EnumerateArray(), item => item.GetProperty("roomId").GetGuid() == roomId);
        Assert.Contains((await Data(await client.GetAsync("/api/admin/reviews"))).EnumerateArray(), item => item.GetProperty("id").GetGuid() == reviewId);
        Assert.Contains((await Data(await client.GetAsync("/api/admin/reports"))).EnumerateArray(), item => item.GetProperty("id").GetGuid() == reportId);
        Assert.Contains((await Data(await client.GetAsync("/api/admin/landlord-requests"))).EnumerateArray(), item => item.GetProperty("id").GetGuid() == requestId);
        Assert.Contains((await Data(await client.GetAsync("/api/admin/universities"))).EnumerateArray(), item => item.GetProperty("id").GetGuid() == universityId);

        var roomStatus = await Data(await client.PatchAsJsonAsync($"/api/admin/rooms/{roomId}/status", new { status = "HIDDEN" }));
        Assert.Equal("HIDDEN", roomStatus.GetProperty("status").GetString());
        var bookingStatus = await Data(await client.PatchAsJsonAsync($"/api/admin/bookings/{bookingId}", new { status = "COMPLETED" }));
        Assert.Equal("COMPLETED", bookingStatus.GetProperty("status").GetString());
        var reviewStatus = await Data(await client.PatchAsJsonAsync($"/api/admin/reviews/{reviewId}", new { isFlagged = true, flagReason = "Admin moderation" }));
        Assert.True(reviewStatus.GetProperty("isFlagged").GetBoolean());

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/admin/favorites/{studentId}/{roomId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/admin/reports/{reportId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/admin/landlord-requests/{requestId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/admin/universities/{universityId}", new
        {
            name = "Trường đã cập nhật",
            shortName = "ADMIN-UPDATED",
            address = "Hà Nội",
            latitude = 21.03,
            longitude = 105.81
        })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/admin/universities/{universityId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/admin/reviews/{reviewId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/admin/bookings/{bookingId}")).StatusCode);
    }
}
