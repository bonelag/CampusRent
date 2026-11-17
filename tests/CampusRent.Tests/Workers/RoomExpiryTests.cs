using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CampusRent.Server.BackgroundServices;
using CampusRent.Server.Data;
using CampusRent.Server.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CampusRent.Tests;
public class RoomExpiryTests : ApiTestBase
{
    [Fact]
    public async Task ExpiryWorkerHidesOnlyExpiredAvailableRooms()
    {
        var id = await Room();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
        var room = (await db.Rooms.FindAsync(id))!;
        room.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();
        Assert.Equal(1, await AutoExpireRoomsWorker.ExpireAsync(db));
        Assert.Equal(RoomStatus.HIDDEN, room.Status);
    }

    [Fact]
    public async Task ConfirmedBookingSendsReminderEmailOnceOnVisitDay()
    {
        var roomId = await Room();
        var ownerHeaders = client.DefaultRequestHeaders.Authorization;
        await Register("STUDENT", "visitor1@example.com");
        var bookingId = (await Data(await client.PostAsJsonAsync($"/api/rooms/{roomId}/bookings", new { visitAt = DateTimeOffset.UtcNow.AddHours(2), note = "Xem phòng" })))
            .GetProperty("id").GetGuid();

        client.DefaultRequestHeaders.Authorization = ownerHeaders;
        await Data(await client.PatchAsJsonAsync($"/api/bookings/{bookingId}", new { status = "CONFIRMED" }));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<CampusRent.Server.Services.IEmailService>();
        await AutoExpireRoomsWorker.ExpireAsync(db, emailService);

        var booking = (await db.Bookings.FindAsync(bookingId))!;
        Assert.NotNull(booking.ConfirmedAt);
        Assert.NotNull(booking.ReminderSentAt);
    }

    [Fact]
    public async Task PostVisitThreeDaysWithoutNotClosedHidesRoom()
    {
        var roomId = await Room();
        var ownerHeaders = client.DefaultRequestHeaders.Authorization;
        await Register("STUDENT", "visitor2@example.com");
        var bookingId = (await Data(await client.PostAsJsonAsync($"/api/rooms/{roomId}/bookings", new { visitAt = DateTimeOffset.UtcNow.AddDays(1), note = "" })))
            .GetProperty("id").GetGuid();

        client.DefaultRequestHeaders.Authorization = ownerHeaders;
        await Data(await client.PatchAsJsonAsync($"/api/bookings/{bookingId}", new { status = "CONFIRMED" }));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
            var b = (await db.Bookings.FindAsync(bookingId))!;
            b.VisitAt = DateTime.UtcNow.AddDays(-3).AddMinutes(-5);
            await db.SaveChangesAsync();
            await AutoExpireRoomsWorker.ExpireAsync(db);

            var room = (await db.Rooms.FindAsync(roomId))!;
            var updatedBooking = (await db.Bookings.FindAsync(bookingId))!;
            Assert.Equal(RoomStatus.HIDDEN, room.Status);
            Assert.NotNull(updatedBooking.AutoClosedAt);
        }
    }

    [Fact]
    public async Task MarkingCustomerNotClosedKeepsRoomVisiblePastThreeDaysAndManualReEnableGrants30Days()
    {
        var roomId = await Room();
        var ownerHeaders = client.DefaultRequestHeaders.Authorization;
        await Register("STUDENT", "visitor3@example.com");
        var bookingId = (await Data(await client.PostAsJsonAsync($"/api/rooms/{roomId}/bookings", new { visitAt = DateTimeOffset.UtcNow.AddDays(1), note = "" })))
            .GetProperty("id").GetGuid();

        client.DefaultRequestHeaders.Authorization = ownerHeaders;
        await Data(await client.PatchAsJsonAsync($"/api/bookings/{bookingId}", new { status = "CONFIRMED" }));
        await Data(await client.PatchAsJsonAsync($"/api/bookings/{bookingId}/not-closed", new { notClosed = true }));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
            var b = (await db.Bookings.FindAsync(bookingId))!;
            b.VisitAt = DateTime.UtcNow.AddDays(-4);
            await db.SaveChangesAsync();
            await AutoExpireRoomsWorker.ExpireAsync(db);

            var room = (await db.Rooms.FindAsync(roomId))!;
            Assert.Equal(RoomStatus.AVAILABLE, room.Status);

            room.ExpiresAt = DateTime.UtcNow.AddDays(-1);
            room.Status = RoomStatus.HIDDEN;
            await db.SaveChangesAsync();
        }

        // Manually re-enabling expired hidden room refreshes 30-day window even without renew=true
        var reactivated = await Data(await client.PatchAsJsonAsync($"/api/rooms/{roomId}/status", new { status = "AVAILABLE", renew = false }));
        Assert.Equal("AVAILABLE", reactivated.GetProperty("status").GetString());
        Assert.True(reactivated.GetProperty("expiresAt").GetDateTime() > DateTime.UtcNow.AddDays(28));
    }
}
