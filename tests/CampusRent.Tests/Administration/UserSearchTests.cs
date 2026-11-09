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
public class UserSearchTests : RoomFeatureTestBase
{
    [Fact]
    public async Task AdminSearchUsersByQueryAndValidation()
    {
        await Register("STUDENT", "nguyen_van_a@example.com");
        await Admin();
        var searchFound = await Data(await client.GetAsync("/api/admin/users?q=nguyen_van_a"));
        Assert.Single(searchFound.EnumerateArray());
        var searchEmpty = await Data(await client.GetAsync("/api/admin/users?q=khong_co_ai_dau_nhe"));
        Assert.Empty(searchEmpty.EnumerateArray());
        // Update non-existent user
        var notFoundUpdate = await client.PutAsJsonAsync($"/api/admin/users/{Guid.NewGuid()}", new { fullName = "Tên", role = "STUDENT", isEduVerified = false });
        Assert.Equal(HttpStatusCode.NotFound, notFoundUpdate.StatusCode);
        // Update with empty name
        var invalidUpdate = await client.PutAsJsonAsync($"/api/admin/users/{Guid.NewGuid()}", new { fullName = "", role = "STUDENT", isEduVerified = false });
        Assert.Equal(HttpStatusCode.BadRequest, invalidUpdate.StatusCode);
    }
}
