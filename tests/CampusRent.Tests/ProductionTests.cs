using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CampusRent.Server.Data;
using CampusRent.Server.Models;
using CampusRent.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace CampusRent.Tests;

public class ProductionTests : IDisposable
{
    private readonly ApiFactory factory = new();
    private readonly HttpClient client;
    public ProductionTests() => client = factory.CreateClient();
    public void Dispose() { client.Dispose(); factory.Dispose(); }

    private static async Task<JsonElement> Data(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}");
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("data").Clone();
    }

    private async Task<(Guid Id, string Token)> Account(Role role = Role.STUDENT)
    {
        var data = await Data(await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = Guid.NewGuid() + "@example.com", password = "TestPassword123!", fullName = "Production test", role = "STUDENT"
        }));
        var id = data.GetProperty("user").GetProperty("id").GetGuid();
        var token = data.GetProperty("token").GetString()!;
        if (role != Role.STUDENT)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
            (await db.Users.FindAsync(id))!.Role = role;
            await db.SaveChangesAsync();
        }
        Use(token);
        return (id, token);
    }

    private void Use(string token) => client.DefaultRequestHeaders.Authorization = new("Bearer", token);

    private async Task<string> Upload(bool pdf)
    {
        using var content = new MultipartFormDataContent();
        var bytes = pdf ? System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n%%EOF") :
            Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jB5kAAAAASUVORK5CYII=");
        content.Add(new ByteArrayContent(bytes), "file", pdf ? "ownership.pdf" : "ownership.png");
        var result = await Data(await client.PostAsync(pdf ? "/api/uploads/pdf" : "/api/uploads/image", content));
        return result.GetProperty("url").GetString()!;
    }

    [Fact]
    public async Task PublicRegistrationCannotBypassLandlordApproval()
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "unapproved@example.com", password = "TestPassword123!", fullName = "Unapproved owner", role = "LANDLORD"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ApprovalAndDemotionApplyToExistingTokens()
    {
        var student = await Account();
        var request = await Data(await client.PostAsJsonAsync("/api/landlord-requests", new { title = "Upgrade account", reason = "I own rental rooms", imageUrls = Array.Empty<string>() }));
        var admin = await Account(Role.ADMIN);
        var requestId = request.GetProperty("id").GetGuid();
        await Data(await client.PatchAsync($"/api/admin/landlord-requests/{requestId}/approve", null));
        Use(student.Token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/rooms/mine")).StatusCode);
        Use(admin.Token);
        await Data(await client.PutAsJsonAsync($"/api/admin/users/{student.Id}", new { fullName = "Student again", role = "STUDENT", isEduVerified = false }));
        Use(student.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/rooms/mine")).StatusCode);
    }

    [Fact]
    public async Task VerificationFilesArePrivateAndAttachmentsCannotReferenceAnotherOwner()
    {
        var owner = await Account();
        var image = await Upload(false);
        var pdf = await Upload(true);
        Assert.StartsWith($"/api/uploads/files/{owner.Id}/", image);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(pdf)).StatusCode);
        var response = await client.GetAsync(image);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(image)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(pdf)).StatusCode);
        await Account();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(image)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(pdf)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/landlord-requests", new { title = "Wrong owner", reason = "Cannot borrow files", imageUrls = new[] { image }, pdfUrl = pdf })).StatusCode);
        await Account(Role.ADMIN);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(image)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(pdf)).StatusCode);
        Use(owner.Token);
        await Data(await client.PostAsJsonAsync("/api/landlord-requests", new { title = "Own documents", reason = "Correct attachments", imageUrls = new[] { image }, pdfUrl = pdf }));
    }

    [Fact]
    public async Task PasswordChangeInvalidatesOldSessionsAndKeepsNewSessionUsable()
    {
        var account = await Account();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/auth/password", new { currentPassword = "WrongPassword!", newPassword = "NewTestPassword123!" })).StatusCode);
        var changed = await Data(await client.PutAsJsonAsync("/api/auth/password", new { currentPassword = "TestPassword123!", newPassword = "NewTestPassword123!" }));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Use(changed.GetProperty("token").GetString()!);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(account.Id, changed.GetProperty("user").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task PendingRequestUniquenessIsEnforcedByTheDatabase()
    {
        var account = await Account();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CampusRentDbContext>();
        db.LandlordRequests.Add(new() { UserId = account.Id, Title = "First", Reason = "First" });
        await db.SaveChangesAsync();
        db.LandlordRequests.Add(new() { UserId = account.Id, Title = "Concurrent", Reason = "Concurrent" });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task LastAdministratorCannotBeDemoted()
    {
        var admin = await Account(Role.ADMIN);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/admin/users/{admin.Id}", new { fullName = "Admin", role = "STUDENT", isEduVerified = false })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/admin/stats")).StatusCode);
    }

    [Fact]
    public void PostgresUrlsPreserveEscapedCredentialsAndUseVerifiedTls()
    {
        var value = new NpgsqlConnectionStringBuilder(PostgresConnection.Normalize("postgresql://owner:p%3Ba%22ss%3Aword@database.example.com/rental%20db?sslmode=verify-full"));
        Assert.Equal("p;a\"ss:word", value.Password);
        Assert.Equal("rental db", value.Database);
        Assert.Equal(SslMode.VerifyFull, value.SslMode);
        var remote = new NpgsqlConnectionStringBuilder(PostgresConnection.Normalize("Host=database.example.com;Database=rooms;Username=owner;Password=test"));
        Assert.Equal(SslMode.VerifyFull, remote.SslMode);
        var explicitLocal = new NpgsqlConnectionStringBuilder(PostgresConnection.Normalize("Host=localhost;Database=rooms;Username=owner;SSL Mode=Disable"));
        Assert.Equal(SslMode.Disable, explicitLocal.SslMode);
    }

    [Fact]
    public async Task PlaceProviderTimeoutsAllowFallbackWithoutHidingClientCancellation()
    {
        using var http = new HttpClient(new TimeoutHandler()) { BaseAddress = new Uri("https://places.example.com/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["GooglePlaces:ApiKey"] = "test-key" }).Build();
        var google = new GooglePlacesClient(http, config, NullLogger<GooglePlacesClient>.Instance);
        var osm = new OsmPlacesClient(http, NullLogger<OsmPlacesClient>.Instance);
        Assert.Empty(await google.SuggestAsync("Bách Khoa", CancellationToken.None));
        Assert.Null(await google.ResolveAsync("places/example", CancellationToken.None));
        Assert.Empty(await osm.SuggestAsync("Bách Khoa", CancellationToken.None));
        Assert.Null(await osm.ReverseGeocodeAsync(21, 105, CancellationToken.None));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => osm.SuggestAsync("Bách Khoa", cancelled.Token));
    }

    private sealed class TimeoutHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(new TaskCanceledException("Provider timeout"));
    }
}
