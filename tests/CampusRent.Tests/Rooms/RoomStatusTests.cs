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
public class RoomStatusTests : RoomFeatureTestBase
{
    [Fact]
    public async Task LandlordCanChangeRoomStatusToRentedAndRenew()
    {
        var roomId = await CreateRoom();
        var landlordToken = client.DefaultRequestHeaders.Authorization;
        // Change to RENTED
        var rentedRes = await Data(await client.PatchAsJsonAsync($"/api/rooms/{roomId}/status", new { status = "RENTED" }));
        Assert.Equal("RENTED", rentedRes.GetProperty("status").GetString());
        // Disappears from public search
        client.DefaultRequestHeaders.Authorization = null;
        var search = await Data(await client.GetAsync("/api/rooms"));
        Assert.DoesNotContain(search.GetProperty("items").EnumerateArray(), r => r.GetProperty("id").GetGuid() == roomId);
        // Landlord re-authenticates and renews
        client.DefaultRequestHeaders.Authorization = landlordToken;
        var renewedRes = await Data(await client.PatchAsJsonAsync($"/api/rooms/{roomId}/status", new { status = "AVAILABLE", renew = true }));
        Assert.Equal("AVAILABLE", renewedRes.GetProperty("status").GetString());
        var expiresAt = renewedRes.GetProperty("expiresAt").GetDateTime();
        Assert.True(expiresAt > DateTime.UtcNow.AddDays(25));
    }
}
