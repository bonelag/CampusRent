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
public class BookingCancellationTests : RoomFeatureTestBase
{
    [Fact]
    public async Task StudentCanCancelTheirOwnBooking()
    {
        var roomId = await CreateRoom();
        await Register("STUDENT");
        var booking = await Data(await client.PostAsJsonAsync($"/api/rooms/{roomId}/bookings", new { visitAt = DateTimeOffset.UtcNow.AddDays(3), note = "Hẹn xem phòng" }));
        var bookingId = booking.GetProperty("id").GetGuid();
        var cancelRes = await client.PatchAsJsonAsync($"/api/bookings/{bookingId}", new { status = "CANCELLED" });
        Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);
        // Cannot update cancelled booking
        var reUpdate = await client.PatchAsJsonAsync($"/api/bookings/{bookingId}", new { status = "CONFIRMED" });
        Assert.Equal(HttpStatusCode.BadRequest, reUpdate.StatusCode);
    }
}
