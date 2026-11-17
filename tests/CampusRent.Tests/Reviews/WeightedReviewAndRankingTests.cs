using System.Net.Http.Json;
using CampusRent.Server.Data;
using CampusRent.Server.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CampusRent.Tests;

public class WeightedReviewAndRankingTests : ApiTestBase
{
    [Fact]
    public async Task VerifiedEduReviewsWeighMoreThanRegularEmails()
    {
        var roomId = await Room();

        await Register("STUDENT", "nonedu@gmail.com", verifyEmail: true);
        await Data(await client.PostAsJsonAsync($"/api/rooms/{roomId}/reviews", new { rating = 1, comment = "Chê phòng này quá tệ" }));

        var eduId = await Register("STUDENT", "sv1@vnu.edu.vn", verifyEmail: true);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
            var eduUser = (await db.Users.FindAsync(eduId))!;
            eduUser.IsEduVerified = true;
            await db.SaveChangesAsync();
        }
        await Data(await client.PostAsJsonAsync($"/api/rooms/{roomId}/reviews", new { rating = 5, comment = "Rất tốt và gần trường" }));

        var detail = await Data(await client.GetAsync($"/api/rooms/{roomId}"));
        var average = detail.GetProperty("averageRating").GetDouble();
        // Weighted: (1*1.0 + 5*2.0) / 3.0 = 11.0 / 3.0 = 3.7
        Assert.InRange(average, 3.6, 3.8);

        var reviews = detail.GetProperty("reviews").EnumerateArray().ToList();
        Assert.Equal(2, reviews.Count);
        Assert.Equal(2.0, reviews[0].GetProperty("weight").GetDouble(), 1);
        Assert.True(reviews[0].GetProperty("user").GetProperty("isEduDomain").GetBoolean());
    }

    [Fact]
    public async Task PoorlyRatedRoomIsDemotedBelowGoodAndUnratedRoomsInDefaultSearch()
    {
        await Register("LANDLORD", "owner1@gmail.com", verifyEmail: true);
        var badRoomId = (await Data(await client.PostAsJsonAsync("/api/rooms", RoomPayload(new { title = "Phòng điểm thấp" }))))
            .GetProperty("id").GetGuid();
        await Register("LANDLORD", "owner2@gmail.com", verifyEmail: true);
        var goodRoomId = (await Data(await client.PostAsJsonAsync("/api/rooms", RoomPayload(new { title = "Phòng điểm cao" }))))
            .GetProperty("id").GetGuid();

        await Register("STUDENT", "reviewer1@gmail.com", verifyEmail: true);
        await Data(await client.PostAsJsonAsync($"/api/rooms/{badRoomId}/reviews", new { rating = 1, comment = "Phòng ẩm thấp" }));
        await Data(await client.PostAsJsonAsync($"/api/rooms/{goodRoomId}/reviews", new { rating = 5, comment = "Phòng sạch sẽ" }));

        var search = await Data(await client.GetAsync("/api/rooms"));
        var items = search.GetProperty("items").EnumerateArray().ToList();

        Assert.Equal(2, items.Count);
        Assert.Equal(goodRoomId, items[0].GetProperty("id").GetGuid());
        Assert.False(items[0].GetProperty("isLowRated").GetBoolean());
        Assert.Equal(badRoomId, items[^1].GetProperty("id").GetGuid());
        Assert.True(items[^1].GetProperty("isLowRated").GetBoolean());
    }
}
