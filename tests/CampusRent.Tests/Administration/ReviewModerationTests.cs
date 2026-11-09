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
public class ReviewModerationTests : ApiTestBase
{
    [Fact]
    public async Task AdminApprovesFlaggedReview()
    {
        var id = await Room();
        await Register();
        var review = await Data(await client.PostAsJsonAsync($"/api/rooms/{id}/reviews", new { rating = 3, comment = "đm review cần kiểm duyệt" }));
        await Admin();
        var flagged = await Data(await client.GetAsync("/api/admin/reviews"));
        Assert.Single(flagged.EnumerateArray());
        await Data(await client.PatchAsync($"/api/admin/reviews/{review.GetProperty("id").GetGuid()}/approve", null));
        Assert.Equal(1, (await Data(await client.GetAsync($"/api/rooms/{id}"))).GetProperty("reviewCount").GetInt32());
    }
}
