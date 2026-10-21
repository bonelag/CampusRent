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
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"campusrent-{Guid.NewGuid()}.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing")
        .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Sqlite"] = $"Data Source={path}", ["Database:Provider"] = "Sqlite" }));
    protected override void Dispose(bool disposing) { base.Dispose(disposing); Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); if (File.Exists(path)) File.Delete(path); }
}
public class ApiTests : IDisposable
{
    private readonly ApiFactory factory = new();
    private readonly HttpClient client;
    public ApiTests() { client = factory.CreateClient(); }
    public void Dispose() { client.Dispose(); factory.Dispose(); }
    private async Task<JsonElement> Data(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement.GetProperty("data").Clone();
    }
    private async Task<Guid> Register(string role = "STUDENT", string? email = null)
    {
        var data = await Data(await client.PostAsJsonAsync("/api/auth/register", new { email = email ?? $"{Guid.NewGuid()}@example.com", password = "TestPass123!", fullName = "Test User", role }));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.GetProperty("token").GetString());
        return data.GetProperty("user").GetProperty("id").GetGuid();
    }
    private object RoomPayload(object? overrides = null) => new { title = "Studio gần trường đại học", description = "Phòng đầy đủ tiện nghi", address = "15 Tạ Quang Bửu, Hà Nội", latitude = 21.005, longitude = 105.843, basePrice = 3200000, electricPrice = 3800, waterPrice = 30000, internetPrice = 100000, otherFee = 50000, area = 25, maxPeople = 2, amenities = new[] { "wifi", "ac" }, imageUrls = new[] { "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800" } };
    private async Task<Guid> Room()
    {
        await Register("LANDLORD");
        var data = await Data(await client.PostAsJsonAsync("/api/rooms", RoomPayload()));
        return data.GetProperty("id").GetGuid();
    }
    [Fact] public async Task HealthWorks() => Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    [Fact] public async Task AuthenticationRequired() => Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    [Fact] public async Task RegistrationCannotEscalateToAdmin() => Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/register", new { email = "hacker@example.com", password = "TestPass123!", fullName = "Hacker", role = "ADMIN" })).StatusCode);
    [Fact] public async Task InvalidRegistrationIsRejected() => Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/register", new { email = "invalid", password = "123", fullName = "" })).StatusCode);
    [Fact] public async Task AcademicEmailIsNotAutomaticallyVerified()
    {
        await Register(email: "student@hust.edu.vn");
        var user = await Data(await client.GetAsync("/api/auth/me"));
        Assert.False(user.GetProperty("isEduVerified").GetBoolean());
    }
    [Fact] public async Task EmailNormalizationAndDuplicateCheck()
    {
        await Register(email: "Student@Example.com");
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/register", new { email = "student@example.com", password = "TestPass123!", fullName = "Other" })).StatusCode);
        var data = await Data(await client.PostAsJsonAsync("/api/auth/login", new { email = "STUDENT@example.com", password = "TestPass123!" }));
        Assert.Equal("student@example.com", data.GetProperty("user").GetProperty("email").GetString());
    }
    [Fact] public async Task InvalidPasswordDoesNotAuthenticate()
    {
        await Register(email: "student@example.com");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { email = "student@example.com", password = "wrong" })).StatusCode);
    }
    [Fact] public async Task StudentCannotPublishOrAccessAdmin()
    {
        await Register();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/rooms", RoomPayload())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/stats")).StatusCode);
    }
    [Fact] public async Task PublishSearchDetailAndEstimate()
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
    [Fact] public async Task OwnerCanUpdateRoomAndReplaceImages()
    {
        var id = await Room();
        await Data(await client.PutAsJsonAsync($"/api/rooms/{id}", RoomPayload()));
        var detail = await Data(await client.GetAsync($"/api/rooms/{id}"));
        Assert.Single(detail.GetProperty("images").EnumerateArray());
    }
    [Fact] public async Task OtherLandlordCannotEditArchiveOrChangeStatus()
    {
        var id = await Room(); await Register("LANDLORD");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/rooms/{id}", RoomPayload())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/rooms/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync($"/api/rooms/{id}/status", new { status = "RENTED" })).StatusCode);
    }
    [Fact] public async Task HiddenRoomsDisappearFromPublicSearchAndDetail()
    {
        var id = await Room();
        await Data(await client.DeleteAsync($"/api/rooms/{id}"));
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/rooms/{id}")).StatusCode);
        Assert.Equal(0, (await Data(await client.GetAsync("/api/rooms"))).GetProperty("total").GetInt32());
    }
    [Fact] public async Task FavoriteOperationsAreIdempotent()
    {
        var id = await Room(); await Register();
        await Data(await client.PutAsync($"/api/favorites/{id}", null));
        await Data(await client.PutAsync($"/api/favorites/{id}", null));
        Assert.Equal(1, (await Data(await client.GetAsync("/api/favorites"))).GetArrayLength());
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/favorites/{id}")).StatusCode);
        Assert.Equal(0, (await Data(await client.GetAsync("/api/favorites"))).GetArrayLength());
    }
    [Fact] public async Task ReviewsEnforceUniquenessAndModeration()
    {
        var id = await Room(); await Register();
        await Data(await client.PostAsJsonAsync($"/api/rooms/{id}/reviews", new { rating = 1, comment = "đm phòng này" }));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/rooms/{id}/reviews", new { rating = 5, comment = "rất tốt" })).StatusCode);
        Assert.Equal(0, (await Data(await client.GetAsync($"/api/rooms/{id}"))).GetProperty("reviewCount").GetInt32());
    }
    [Fact] public async Task ReviewRatingMustBeValid()
    {
        var id = await Room(); await Register();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/rooms/{id}/reviews", new { rating = 6, comment = "tốt" })).StatusCode);
    }
    [Fact] public async Task BookingRequiresFutureDateAndOwnerConfirms()
    {
        var id = await Room(); var ownerToken = client.DefaultRequestHeaders.Authorization;
        await Register();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/rooms/{id}/bookings", new { visitAt = DateTimeOffset.UtcNow.AddDays(-1), note = "" })).StatusCode);
        var booking = await Data(await client.PostAsJsonAsync($"/api/rooms/{id}/bookings", new { visitAt = DateTimeOffset.UtcNow.AddDays(1), note = "Xem phòng buổi chiều" }));
        var bookingId = booking.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync($"/api/bookings/{bookingId}", new { status = "CONFIRMED" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/rooms/{id}/bookings", new { visitAt = DateTimeOffset.UtcNow.AddDays(2), note = "" })).StatusCode);
        client.DefaultRequestHeaders.Authorization = ownerToken;
        await Data(await client.PatchAsJsonAsync($"/api/bookings/{bookingId}", new { status = "CONFIRMED" }));
        Assert.Equal("CONFIRMED", (await Data(await client.GetAsync("/api/bookings")))[0].GetProperty("status").GetString());
    }
    [Fact] public async Task UnrelatedUserCannotCancelBooking()
    {
        var id = await Room(); await Register();
        var b = await Data(await client.PostAsJsonAsync($"/api/rooms/{id}/bookings", new { visitAt = DateTimeOffset.UtcNow.AddDays(1) }));
        await Register();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync($"/api/bookings/{b.GetProperty("id").GetGuid()}", new { status = "CANCELLED" })).StatusCode);
    }
    [Fact] public async Task ReportsRequireExactlyOneExistingTarget()
    {
        var id = await Room(); await Register();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/reports", new { reason = "Tin giả" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/reports", new { roomId = Guid.NewGuid(), reason = "Tin giả" })).StatusCode);
        await Data(await client.PostAsJsonAsync("/api/reports", new { roomId = id, reason = "Thông tin sai" }));
    }
    [Fact] public async Task ExpiryWorkerHidesOnlyExpiredAvailableRooms()
    {
        var id = await Room();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
        var room = (await db.Rooms.FindAsync(id))!; room.ExpiresAt = DateTime.UtcNow.AddDays(-1); await db.SaveChangesAsync();
        Assert.Equal(1, await AutoExpireRoomsWorker.ExpireAsync(db));
        Assert.Equal(RoomStatus.HIDDEN, room.Status);
    }
    [Theory]
    [InlineData("/api/rooms?page=0")]
    [InlineData("/api/rooms?pageSize=101")]
    [InlineData("/api/rooms?centerLat=100&centerLon=0")]
    [InlineData("/api/rooms?radius=3")]
    public async Task SearchRejectsInvalidFilters(string url) => Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(url)).StatusCode);

    [Fact] public async Task PlaceSuggestionsReturnKnownListingCoordinatesAndValidateInput()
    {
        await Room();
        client.DefaultRequestHeaders.Authorization = null;
        var suggestions = await Data(await client.GetAsync("/api/places/suggest?q=T%E1%BA%A1%20Quang"));
        var suggestion = suggestions.EnumerateArray().Single();
        Assert.Equal("listing", suggestion.GetProperty("source").GetString());
        Assert.Equal(21.005, suggestion.GetProperty("latitude").GetDouble());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/places/suggest?q=x")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/places/suggest?q=" + new string('x', 121))).StatusCode);
    }

    private async Task Admin()
    {
        var id = await Register("LANDLORD");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
        var user = (await db.Users.FindAsync(id))!; user.Role = Role.ADMIN; await db.SaveChangesAsync();
        var token = scope.ServiceProvider.GetRequiredService<CampusRent.Server.Services.ITokenService>().GenerateToken(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
    [Fact] public async Task AdminResolvesReportAndHidesTarget()
    {
        var id = await Room(); await Register();
        var report = await Data(await client.PostAsJsonAsync("/api/reports", new { roomId = id, reason = "Tin không đúng" }));
        await Admin();
        var rid = report.GetProperty("id").GetGuid();
        await Data(await client.PatchAsJsonAsync($"/api/admin/reports/{rid}", new { status = "RESOLVED", deleteTarget = true }));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PatchAsJsonAsync($"/api/admin/reports/{rid}", new { status = "RESOLVED" })).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/rooms/{id}")).StatusCode);
    }
    [Fact] public async Task AdminApprovesFlaggedReview()
    {
        var id = await Room(); await Register();
        var review = await Data(await client.PostAsJsonAsync($"/api/rooms/{id}/reviews", new { rating = 3, comment = "đm review cần kiểm duyệt" }));
        await Admin();
        var flagged = await Data(await client.GetAsync("/api/admin/reviews"));
        Assert.Single(flagged.EnumerateArray());
        await Data(await client.PatchAsync($"/api/admin/reviews/{review.GetProperty("id").GetGuid()}/approve", null));
        Assert.Equal(1, (await Data(await client.GetAsync($"/api/rooms/{id}"))).GetProperty("reviewCount").GetInt32());
    }
    [Fact] public async Task UniversityCreationValidatesCoordinatesAndDuplicates()
    {
        await Admin();
        var payload = new { name = "Đại học kiểm thử", shortName = "TEST", address = "Hà Nội", latitude = 21, longitude = 105 };
        await Data(await client.PostAsJsonAsync("/api/universities", payload));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/universities", payload)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/universities", new { name="Wrong", shortName="WRONG", address="test", latitude=91,longitude=0 })).StatusCode);
    }
    [Fact] public async Task ProfileCanBeUpdatedWithoutLeakingPassword()
    {
        await Register();
        var profile = await Data(await client.PutAsJsonAsync("/api/auth/me", new { fullName="Tên đã sửa",phoneNumber="0912345678" }));
        Assert.Equal("Tên đã sửa", profile.GetProperty("fullName").GetString());
        Assert.False(profile.TryGetProperty("password", out _));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/auth/me", new { fullName="" })).StatusCode);
    }
    [Fact] public async Task CancelledBookingCanBeRebookedAtSameTime()
    {
        var id = await Room(); await Register();
        var visit = DateTimeOffset.UtcNow.AddDays(2);
        var b = await Data(await client.PostAsJsonAsync($"/api/rooms/{id}/bookings", new { visitAt=visit,note="" }));
        await Data(await client.PatchAsJsonAsync($"/api/bookings/{b.GetProperty("id").GetGuid()}", new {status="CANCELLED"}));
        await Data(await client.PostAsJsonAsync($"/api/rooms/{id}/bookings", new {visitAt=visit,note=""}));
    }
    [Fact] public async Task BookingDatesAreReturnedAsUtc()
    {
        var id = await Room(); await Register();
        await Data(await client.PostAsJsonAsync($"/api/rooms/{id}/bookings", new { visitAt=DateTimeOffset.UtcNow.AddDays(2),note="" }));
        var bookings = await Data(await client.GetAsync("/api/bookings"));
        Assert.EndsWith("Z", bookings[0].GetProperty("visitAt").GetString());
    }
    [Fact] public async Task InvalidJwtCannotAccessProtectedRoute()
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer","invalid.token.value");
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/auth/me")).StatusCode);
    }
    [Fact] public async Task LoginAttemptsAreRateLimited()
    {
        HttpResponseMessage? response = null;
        for (var i=0;i<21;i++) response = await client.PostAsJsonAsync("/api/auth/login",new {email="none@example.com",password="wrong"});
        Assert.Equal(HttpStatusCode.TooManyRequests,response!.StatusCode);
    }
    [Fact] public async Task UnicodePasswordCannotExceedBcryptByteLimit()
    {
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/auth/register",new {email="test@example.com",password=new string('ệ',30),fullName="Test"})).StatusCode);
    }
    [Fact] public async Task UploadRejectsDisguisedFiles()
    {
        await Register("LANDLORD");
        using var content=new MultipartFormDataContent();
        content.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("<script>alert(1)</script>")),"file","fake.jpg");
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsync("/api/images",content)).StatusCode);
    }
    [Fact] public async Task RoomPayloadRejectsUnsafeImageUrls()
    {
        await Register("LANDLORD");
        var payload=JsonSerializer.SerializeToNode(RoomPayload())!;
        payload["imageUrls"]=new System.Text.Json.Nodes.JsonArray("javascript:alert(1)");
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/rooms",payload)).StatusCode);
    }
    [Fact] public async Task OwnerCannotReviewOwnListing()
    {
        var id=await Room();
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync($"/api/rooms/{id}/reviews",new {rating=5,comment="Tự đánh giá"})).StatusCode);
    }

    [Fact] public async Task DemoSeedIsIdempotentAndAdminStatisticsMatch()
    {
        using (var scope=factory.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
            await DbSeeder.SeedAsync(db); await DbSeeder.SeedAsync(db);
            Assert.Equal(5,await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.Rooms));
            Assert.Equal(5,await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.Users));
            Assert.Equal(5,await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.Universities));
        }
        await Admin();
        var stats=await Data(await client.GetAsync("/api/admin/stats"));
        Assert.Equal(5,stats.GetProperty("rooms").GetInt32());
        Assert.Equal(5,stats.GetProperty("availableRooms").GetInt32());
        Assert.Equal(0,(await Data(await client.GetAsync("/api/admin/reports"))).GetArrayLength());
    }
    [Fact] public async Task ValidImageWithoutStorageConfigurationFailsClearly()
    {
        await Register("LANDLORD");
        using var content=new MultipartFormDataContent();
        content.Add(new ByteArrayContent(new byte[]{137,80,78,71,13,10,26,10,0,0,0,0}),"file","image.png");
        Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsync("/api/images",content)).StatusCode);
    }
}
