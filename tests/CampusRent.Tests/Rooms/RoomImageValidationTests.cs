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
public class RoomImageValidationTests : ApiTestBase
{
    [Fact]
    public async Task RoomPayloadRejectsUnsafeImageUrls()
    {
        await Register("LANDLORD");
        var payload = JsonSerializer.SerializeToNode(RoomPayload())!;
        payload["imageUrls"] = new System.Text.Json.Nodes.JsonArray("javascript:alert(1)");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/rooms", payload)).StatusCode);
    }
}
