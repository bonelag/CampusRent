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
public class RoomPublishingTests : ApiTestBase
{
    [Fact]
    public async Task PublishSearchDetailAndEstimate()
    {
        var id = await Room();
        var detail = await Data(await client.GetAsync($"/api/rooms/{id}"));
        Assert.Equal(25, detail.GetProperty("area").GetDouble());
        Assert.False(detail.GetProperty("landlord").TryGetProperty("password", out _));
        var search = await Data(await client.GetAsync("/api/rooms?maxPrice=4000000&centerLat=21.005&centerLon=105.843&radius=1"));
        Assert.Equal(1, search.GetProperty("total").GetInt32());
        var estimate = await Data(await client.GetAsync($"/api/rooms/{id}/cost-estimate"));
        Assert.Equal(3774000, estimate.GetProperty("totalEstimatedMonthlyCost").GetDecimal());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/rooms/{id}/cost-estimate?expectedKwh=-1")).StatusCode);
    }
}
