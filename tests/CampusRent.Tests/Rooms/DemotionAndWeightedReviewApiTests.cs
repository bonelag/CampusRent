using System.Net.Http.Json;
using System.Text.Json;
using CampusRent.Server.Data;
using CampusRent.Server.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CampusRent.Tests;

public class DemotionAndWeightedReviewApiTests : RoomFeatureTestBase
{
    [Fact]
    public async Task WeightedReviewCalculation_AppliedCorrectlyInRoomDetail()
    {
        var roomId = await CreateRoom(title: "Phòng trọ Bách Khoa");

        await Register(role: "STUDENT", email: $"regular_{Guid.NewGuid()}@gmail.com");
        var res1 = await client.PostAsJsonAsync($"/api/rooms/{roomId}/reviews", new
        {
            rating = 2,
            comment = "Phòng hơi nhỏ và cũ."
        });
        var data1 = await Data(res1);
        Assert.Equal(1.0, data1.GetProperty("weight").GetDouble());

        await Register(role: "STUDENT", email: $"student_{Guid.NewGuid()}@hust.edu.vn");
        var res2 = await client.PostAsJsonAsync($"/api/rooms/{roomId}/reviews", new
        {
            rating = 5,
            comment = "Chủ trọ nhiệt tình, vị trí đi học rất tiện lợi!"
        });
        var data2 = await Data(res2);
        Assert.Equal(2.0, data2.GetProperty("weight").GetDouble());

        client.DefaultRequestHeaders.Authorization = null;
        var detailRes = await Data(await client.GetAsync($"/api/rooms/{roomId}"));

        var avgRating = detailRes.GetProperty("averageRating").GetDouble();
        Assert.Equal(4.0, avgRating);
        Assert.Equal(2, detailRes.GetProperty("reviewCount").GetInt32());

        var reviews = detailRes.GetProperty("reviews").EnumerateArray().ToList();
        Assert.Equal(2, reviews.Count);

        var studentReview = reviews.First(r => r.GetProperty("rating").GetInt32() == 5);
        Assert.Equal(2.0, studentReview.GetProperty("weight").GetDouble());
        Assert.True(studentReview.GetProperty("user").GetProperty("isEduVerified").GetBoolean());

        var regularReview = reviews.First(r => r.GetProperty("rating").GetInt32() == 2);
        Assert.Equal(1.0, regularReview.GetProperty("weight").GetDouble());
        Assert.False(regularReview.GetProperty("user").GetProperty("isEduVerified").GetBoolean());
    }

    [Fact]
    public async Task DemotionRanking_PushesDemotedRoomToEndOfSearchList()
    {
        var goodRoomId = await CreateRoom(basePrice: 5000000, title: "Phòng VIP tiện nghi cao cấp");

        await Register(role: "STUDENT", email: $"good_{Guid.NewGuid()}@hust.edu.vn");
        await client.PostAsJsonAsync($"/api/rooms/{goodRoomId}/reviews", new
        {
            rating = 5,
            comment = "Phòng rất đẹp và sạch sẽ!"
        });

        var badRoomId = await CreateRoom(basePrice: 2000000, title: "Phòng ẩm thấp giá rẻ");

        await Register(role: "STUDENT", email: $"unhappy1_{Guid.NewGuid()}@gmail.com");
        await client.PostAsJsonAsync($"/api/rooms/{badRoomId}/reviews", new
        {
            rating = 1,
            comment = "Rất tệ hại, phòng dột nước."
        });

        await Register(role: "STUDENT", email: $"unhappy2_{Guid.NewGuid()}@gmail.com");
        await client.PostAsJsonAsync($"/api/rooms/{badRoomId}/reviews", new
        {
            rating = 2,
            comment = "Thất vọng, không như mô tả."
        });

        client.DefaultRequestHeaders.Authorization = null;
        var searchRes = await Data(await client.GetAsync("/api/rooms?sort=price_asc"));
        var items = searchRes.GetProperty("items").EnumerateArray().ToList();

        var badItemIndex = items.FindIndex(r => r.GetProperty("id").GetGuid() == badRoomId);
        var goodItemIndex = items.FindIndex(r => r.GetProperty("id").GetGuid() == goodRoomId);

        Assert.True(badItemIndex >= 0, "Bad room should be in search results");
        Assert.True(goodItemIndex >= 0, "Good room should be in search results");

        Assert.True(goodItemIndex < badItemIndex,
            $"Good room at index {goodItemIndex} should appear before demoted bad room at index {badItemIndex}");

        var badItem = items[badItemIndex];
        Assert.True(badItem.GetProperty("isDemoted").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(badItem.GetProperty("demotionReason").GetString()));
    }
}
