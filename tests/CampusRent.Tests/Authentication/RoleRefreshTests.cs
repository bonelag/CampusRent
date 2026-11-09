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
public class RoleRefreshTests : SecurityTestBase
{
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
}
