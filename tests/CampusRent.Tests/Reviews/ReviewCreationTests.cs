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
public class ReviewCreationTests : ApiTestBase
{
    [Fact]
    public async Task ReviewsEnforceUniquenessAndModeration()
    {
        var id = await Room();
        await Register();
        await Data(await client.PostAsJsonAsync($"/api/rooms/{id}/reviews", new { rating = 1, comment = "đm phòng này" }));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/rooms/{id}/reviews", new { rating = 5, comment = "rất tốt" })).StatusCode);
        Assert.Equal(0, (await Data(await client.GetAsync($"/api/rooms/{id}"))).GetProperty("reviewCount").GetInt32());
    }

    [Fact]
    public async Task ReviewRatingMustBeValid()
    {
        var id = await Room();
        await Register();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/rooms/{id}/reviews", new { rating = 6, comment = "tốt" })).StatusCode);
    }

    [Fact]
    public async Task OwnerCannotReviewOwnListing()
    {
        var id = await Room();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/rooms/{id}/reviews", new { rating = 5, comment = "Tự đánh giá" })).StatusCode);
    }
}
