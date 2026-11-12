using CampusRent.Server.Data;
using CampusRent.Server.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CampusRent.Tests;

public class AdminStatsTests : ApiTestBase
{
    [Fact]
    public async Task RoomDistributionIsDisjointAndPendingBookingsJoinTheBacklog()
    {
        var availableId = await Room();
        var rentedId = await Room();
        var expiredId = await Room();
        var studentId = await Register();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
            var rented = (await db.Rooms.FindAsync(rentedId))!;
            rented.Status = RoomStatus.RENTED;
            rented.ExpiresAt = DateTime.UtcNow.AddDays(-1);
            var expired = (await db.Rooms.FindAsync(expiredId))!;
            expired.ExpiresAt = DateTime.UtcNow.AddDays(-1);
            db.Bookings.Add(new Booking
            {
                RoomId = availableId,
                StudentId = studentId,
                VisitAt = DateTime.UtcNow.AddDays(2)
            });
            await db.SaveChangesAsync();
        }

        await Admin();
        var stats = await Data(await client.GetAsync("/api/admin/stats"));

        Assert.Equal(3, stats.GetProperty("rooms").GetInt32());
        Assert.Equal(1, stats.GetProperty("availableRooms").GetInt32());
        Assert.Equal(1, stats.GetProperty("rentedRooms").GetInt32());
        Assert.Equal(1, stats.GetProperty("hiddenRooms").GetInt32());
        Assert.Equal(1, stats.GetProperty("pendingBookings").GetInt32());
        Assert.Equal(1, stats.GetProperty("moderationBacklog").GetInt32());
    }
}
