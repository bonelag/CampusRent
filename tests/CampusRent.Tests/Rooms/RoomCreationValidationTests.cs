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
public class RoomCreationValidationTests : RoomFeatureTestBase
{
    [Fact]
    public async Task CreateRoomValidationRejectsInvalidInputs()
    {
        await Register("LANDLORD");
        // Non-existent university
        var p1 = JsonSerializer.SerializeToNode(RoomPayload())!;
        p1["universityId"] = Guid.NewGuid().ToString();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/rooms", p1)).StatusCode);
        // Empty images
        var p2 = JsonSerializer.SerializeToNode(RoomPayload())!;
        p2["imageUrls"] = new System.Text.Json.Nodes.JsonArray();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/rooms", p2)).StatusCode);
        // HTTP image (not HTTPS)
        var p3 = JsonSerializer.SerializeToNode(RoomPayload())!;
        p3["imageUrls"] = new System.Text.Json.Nodes.JsonArray("http://insecure.example.com/img.jpg");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/rooms", p3)).StatusCode);
        // More than 10 images
        var p4 = JsonSerializer.SerializeToNode(RoomPayload())!;
        var arr = new System.Text.Json.Nodes.JsonArray();
        for (int i = 0; i < 11; i++)
            arr.Add($"https://example.com/img{i}.jpg");
        p4["imageUrls"] = arr;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/rooms", p4)).StatusCode);
        // Too many amenities (> 20)
        var p5 = JsonSerializer.SerializeToNode(RoomPayload())!;
        var amArr = new System.Text.Json.Nodes.JsonArray();
        for (int i = 0; i < 21; i++)
            amArr.Add($"amenity_{i}");
        p5["amenities"] = amArr;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/rooms", p5)).StatusCode);
    }
}
