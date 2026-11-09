using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CampusRent.Server.Data;
using CampusRent.Server.Models;
using CampusRent.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusRent.Tests;
public class BookingValidationTests : RoomFeatureTestBase
{
    [Fact]
    public async Task StudentCannotBookOwnRoom()
    {
        var roomId = await CreateRoom();
        var res = await client.PostAsJsonAsync($"/api/rooms/{roomId}/bookings", new { visitAt = DateTimeOffset.UtcNow.AddDays(2), note = "Tự đặt phòng của mình" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task BookingValidationRejectsInvalidDates()
    {
        var roomId = await CreateRoom();
        await Register("STUDENT");
        // Past or less than 1 hour in future
        var pastRes = await client.PostAsJsonAsync($"/api/rooms/{roomId}/bookings", new { visitAt = DateTimeOffset.UtcNow.AddMinutes(30) });
        Assert.Equal(HttpStatusCode.BadRequest, pastRes.StatusCode);
        // More than 30 days in future
        var futureRes = await client.PostAsJsonAsync($"/api/rooms/{roomId}/bookings", new { visitAt = DateTimeOffset.UtcNow.AddDays(35) });
        Assert.Equal(HttpStatusCode.BadRequest, futureRes.StatusCode);
        // Booking non-existent room
        var notFoundRes = await client.PostAsJsonAsync($"/api/rooms/{Guid.NewGuid()}/bookings", new { visitAt = DateTimeOffset.UtcNow.AddDays(2) });
        Assert.Equal(HttpStatusCode.NotFound, notFoundRes.StatusCode);
    }

    [Fact]
    public async Task InvalidBookingStatusIsRejected()
    {
        var roomId = await CreateRoom();
        await Register("STUDENT");
        var booking = await Data(await client.PostAsJsonAsync($"/api/rooms/{roomId}/bookings", new { visitAt = DateTimeOffset.UtcNow.AddDays(3) }));
        var bookingId = booking.GetProperty("id").GetGuid();
        var res = await client.PatchAsJsonAsync($"/api/bookings/{bookingId}", new { status = "INVALID_STATUS" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
