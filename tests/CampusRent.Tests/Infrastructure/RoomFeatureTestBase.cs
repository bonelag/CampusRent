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
public abstract class RoomFeatureTestBase : ApiTestBase
{
    protected override object RoomPayload(object? overrides = null) => new
    {
        title = "Phòng trọ cao cấp Hai Bà Trưng",
        description = "Phòng khép kín đầy đủ đồ",
        address = "15 Tạ Quang Bửu, Hà Nội",
        latitude = 21.005,
        longitude = 105.843,
        basePrice = 3000000,
        electricPrice = 4000,
        waterPrice = 30000,
        internetPrice = 100000,
        otherFee = 50000,
        area = 28,
        maxPeople = 2,
        amenities = new[]
        {
            "wifi",
            "ac",
            "kitchen"
        },
        imageUrls = new[]
        {
            "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800"
        }
    };
    protected async Task<Guid> CreateRoom(decimal basePrice = 3000000, string[]? amenities = null, string? title = null)
    {
        await Register("LANDLORD");
        var payload = new
        {
            title = title ?? "Phòng trọ cao cấp Hai Bà Trưng",
            description = "Phòng khép kín đầy đủ đồ",
            address = "15 Tạ Quang Bửu, Hà Nội",
            latitude = 21.005,
            longitude = 105.843,
            basePrice,
            electricPrice = 4000,
            waterPrice = 30000,
            internetPrice = 100000,
            otherFee = 50000,
            area = 28,
            maxPeople = 2,
            amenities = amenities ?? new[]
            {
                "wifi",
                "ac"
            },
            imageUrls = new[]
            {
                "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800"
            }
        };
        var data = await Data(await client.PostAsJsonAsync("/api/rooms", payload));
        return data.GetProperty("id").GetGuid();
    }
}
