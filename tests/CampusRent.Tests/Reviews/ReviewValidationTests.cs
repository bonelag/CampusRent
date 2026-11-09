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
public class ReviewValidationTests : RoomFeatureTestBase
{
    [Fact]
    public async Task ReviewingNonExistentOrHiddenRoomReturnsNotFound()
    {
        await Register("STUDENT");
        var notFound = await client.PostAsJsonAsync($"/api/rooms/{Guid.NewGuid()}/reviews", new { rating = 5, comment = "Phòng ảo" });
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        var roomId = await CreateRoom();
        await client.DeleteAsync($"/api/rooms/{roomId}"); // Hidden
        await Register("STUDENT", "student_reviewer@example.com");
        var hiddenReview = await client.PostAsJsonAsync($"/api/rooms/{roomId}/reviews", new { rating = 5, comment = "Phòng đã ẩn" });
        Assert.Equal(HttpStatusCode.NotFound, hiddenReview.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    public async Task ReviewRatingOutOfRangeIsRejected(int rating)
    {
        var roomId = await CreateRoom();
        await Register("STUDENT");
        var res = await client.PostAsJsonAsync($"/api/rooms/{roomId}/reviews", new { rating, comment = "Đánh giá thử nghiệm" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task CleanReviewsComputeAverageRatingAndReviewCount()
    {
        var roomId = await CreateRoom();
        // Student 1 leaves 4 stars
        await Register("STUDENT", "student_one@example.com");
        await client.PostAsJsonAsync($"/api/rooms/{roomId}/reviews", new { rating = 4, comment = "Phòng khá đẹp và sạch sẽ" });
        // Student 2 leaves 5 stars
        await Register("STUDENT", "student_two@example.com");
        await client.PostAsJsonAsync($"/api/rooms/{roomId}/reviews", new { rating = 5, comment = "Chủ trọ nhiệt tình chu đáo" });
        client.DefaultRequestHeaders.Authorization = null;
        var detail = await Data(await client.GetAsync($"/api/rooms/{roomId}"));
        Assert.Equal(2, detail.GetProperty("reviewCount").GetInt32());
        Assert.Equal(4.5, detail.GetProperty("averageRating").GetDouble());
    }
}
