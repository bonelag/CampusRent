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
public class RoomSearchTests : RoomFeatureTestBase
{
    [Fact]
    public async Task RoomSearchSortingByPriceAscendingAndDescending()
    {
        await CreateRoom(basePrice: 1500000, title: "Phòng giá rẻ");
        await CreateRoom(basePrice: 4500000, title: "Phòng cao cấp");
        client.DefaultRequestHeaders.Authorization = null;
        var ascRes = await Data(await client.GetAsync("/api/rooms?sort=price_asc"));
        var ascItems = ascRes.GetProperty("items").EnumerateArray().ToList();
        Assert.True(ascItems.Count >= 2);
        Assert.True(ascItems[0].GetProperty("basePrice").GetDecimal() <= ascItems[1].GetProperty("basePrice").GetDecimal());
        var descRes = await Data(await client.GetAsync("/api/rooms?sort=price_desc"));
        var descItems = descRes.GetProperty("items").EnumerateArray().ToList();
        Assert.True(descItems.Count >= 2);
        Assert.True(descItems[0].GetProperty("basePrice").GetDecimal() >= descItems[1].GetProperty("basePrice").GetDecimal());
    }

    [Fact]
    public async Task RoomSearchFilterByAmenity()
    {
        var roomId = await CreateRoom(amenities: new[] { "wifi", "ac", "kitchen" }, title: "Phòng có bếp riêng");
        client.DefaultRequestHeaders.Authorization = null;
        var withKitchen = await Data(await client.GetAsync("/api/rooms?amenity=kitchen"));
        Assert.Contains(withKitchen.GetProperty("items").EnumerateArray(), r => r.GetProperty("id").GetGuid() == roomId);
        var withPool = await Data(await client.GetAsync("/api/rooms?amenity=swimming_pool"));
        Assert.DoesNotContain(withPool.GetProperty("items").EnumerateArray(), r => r.GetProperty("id").GetGuid() == roomId);
    }

    [Fact]
    public async Task RoomSearchFilterByKeywordInTitleOrAddress()
    {
        var roomId = await CreateRoom(title: "Chung cư mini vip Hoàng Mai");
        client.DefaultRequestHeaders.Authorization = null;
        var found = await Data(await client.GetAsync("/api/rooms?keyword=Ho%C3%A0ng%20Mai"));
        Assert.Contains(found.GetProperty("items").EnumerateArray(), r => r.GetProperty("id").GetGuid() == roomId);
        var notFound = await Data(await client.GetAsync("/api/rooms?keyword=KhongCoPhongNayDau999"));
        Assert.Equal(0, notFound.GetProperty("total").GetInt32());
    }
}
