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
public class BookingLifecycleTests : ApiTestBase
{
    [Fact]
    public async Task BookingRequiresFutureDateAndOwnerConfirms()
    {
        var id = await Room();
        var ownerToken = client.DefaultRequestHeaders.Authorization;
        await Register();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/rooms/{id}/bookings", new { visitAt = DateTimeOffset.UtcNow.AddDays(-1), note = "" })).StatusCode);
        var booking = await Data(await client.PostAsJsonAsync($"/api/rooms/{id}/bookings", new { visitAt = DateTimeOffset.UtcNow.AddDays(1), note = "Xem phòng buổi chiều" }));
        var bookingId = booking.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync($"/api/bookings/{bookingId}", new { status = "CONFIRMED" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/rooms/{id}/bookings", new { visitAt = DateTimeOffset.UtcNow.AddDays(2), note = "" })).StatusCode);
        client.DefaultRequestHeaders.Authorization = ownerToken;
        await Data(await client.PatchAsJsonAsync($"/api/bookings/{bookingId}", new { status = "CONFIRMED" }));
        Assert.Equal("CONFIRMED", (await Data(await client.GetAsync("/api/bookings")))[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task UnrelatedUserCannotCancelBooking()
    {
        var id = await Room();
        await Register();
        var b = await Data(await client.PostAsJsonAsync($"/api/rooms/{id}/bookings", new { visitAt = DateTimeOffset.UtcNow.AddDays(1) }));
        await Register();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync($"/api/bookings/{b.GetProperty("id").GetGuid()}", new { status = "CANCELLED" })).StatusCode);
    }

    [Fact]
    public async Task CancelledBookingCanBeRebookedAtSameTime()
    {
        var id = await Room();
        await Register();
        var visit = DateTimeOffset.UtcNow.AddDays(2);
        var b = await Data(await client.PostAsJsonAsync($"/api/rooms/{id}/bookings", new { visitAt = visit, note = "" }));
        await Data(await client.PatchAsJsonAsync($"/api/bookings/{b.GetProperty("id").GetGuid()}", new { status = "CANCELLED" }));
        await Data(await client.PostAsJsonAsync($"/api/rooms/{id}/bookings", new { visitAt = visit, note = "" }));
    }

    [Fact]
    public async Task BookingDatesAreReturnedAsUtc()
    {
        var id = await Room();
        await Register();
        await Data(await client.PostAsJsonAsync($"/api/rooms/{id}/bookings", new { visitAt = DateTimeOffset.UtcNow.AddDays(2), note = "" }));
        var bookings = await Data(await client.GetAsync("/api/bookings"));
        Assert.EndsWith("Z", bookings[0].GetProperty("visitAt").GetString());
    }
}
