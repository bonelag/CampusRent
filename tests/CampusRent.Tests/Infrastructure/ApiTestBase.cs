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
public abstract class ApiTestBase : IDisposable
{
    protected readonly ApiFactory factory = new();
    protected readonly HttpClient client;
    protected ApiTestBase()
    {
        client = factory.CreateClient();
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }

    protected async Task<JsonElement> Data(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement.GetProperty("data").Clone();
    }

    protected async Task<Guid> Register(string role = "STUDENT", string? email = null)
    {
        var data = await Data(await client.PostAsJsonAsync("/api/auth/register", new { email = email ?? $"{Guid.NewGuid()}@example.com", password = "TestPass123!", fullName = "Test User", role = "STUDENT" }));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.GetProperty("token").GetString());
        var id = data.GetProperty("user").GetProperty("id").GetGuid();
        if (role != "STUDENT")
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
            var account = (await db.Users.FindAsync(id))!;
            account.Role = Enum.Parse<Role>(role);
            await db.SaveChangesAsync();
        }

        return id;
    }

    protected virtual object RoomPayload(object? overrides = null) => new
    {
        title = "Studio gần trường đại học",
        description = "Phòng đầy đủ tiện nghi",
        address = "15 Tạ Quang Bửu, Hà Nội",
        latitude = 21.005,
        longitude = 105.843,
        basePrice = 3200000,
        electricPrice = 3800,
        waterPrice = 30000,
        internetPrice = 100000,
        otherFee = 50000,
        area = 25,
        maxPeople = 2,
        amenities = new[]
        {
            "wifi",
            "ac"
        },
        imageUrls = new[]
        {
            "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800"
        }
    };
    protected async Task<Guid> Room()
    {
        await Register("LANDLORD");
        var data = await Data(await client.PostAsJsonAsync("/api/rooms", RoomPayload()));
        return data.GetProperty("id").GetGuid();
    }

    protected async Task Admin()
    {
        var id = await Register("LANDLORD");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
        var user = (await db.Users.FindAsync(id))!;
        user.Role = Role.ADMIN;
        await db.SaveChangesAsync();
        var token = scope.ServiceProvider.GetRequiredService<CampusRent.Server.Services.ITokenService>().GenerateToken(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
