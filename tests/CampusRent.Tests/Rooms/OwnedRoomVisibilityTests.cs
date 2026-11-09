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
public class OwnedRoomVisibilityTests : RoomFeatureTestBase
{
    [Fact]
    public async Task LandlordCanViewOwnRoomsViaMineEndpoint()
    {
        var roomId = await CreateRoom();
        var myRooms = await Data(await client.GetAsync("/api/rooms/mine"));
        Assert.Contains(myRooms.EnumerateArray(), r => r.GetProperty("id").GetGuid() == roomId);
        // Another landlord has zero rooms
        await Register("LANDLORD", "landlord_two@example.com");
        var otherRooms = await Data(await client.GetAsync("/api/rooms/mine"));
        Assert.Empty(otherRooms.EnumerateArray());
    }

    [Fact]
    public async Task OwnerCanViewTheirOwnHiddenRoomDetailWhilePublicCannot()
    {
        var roomId = await CreateRoom();
        var landlordToken = client.DefaultRequestHeaders.Authorization;
        // Hide room
        await client.DeleteAsync($"/api/rooms/{roomId}");
        // Public/Student gets 404
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/rooms/{roomId}")).StatusCode);
        // Owner gets 200 OK
        client.DefaultRequestHeaders.Authorization = landlordToken;
        var detailRes = await client.GetAsync($"/api/rooms/{roomId}");
        Assert.Equal(HttpStatusCode.OK, detailRes.StatusCode);
    }
}
