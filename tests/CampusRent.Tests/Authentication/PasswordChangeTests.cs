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
public class PasswordChangeTests : SecurityTestBase
{
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
}
