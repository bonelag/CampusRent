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
public class ProfileTests : ApiTestBase
{
    [Fact]
    public async Task ProfileCanBeUpdatedWithoutLeakingPassword()
    {
        await Register();
        var profile = await Data(await client.PutAsJsonAsync("/api/auth/me", new { fullName = "Tên đã sửa", phoneNumber = "0912345678" }));
        Assert.Equal("Tên đã sửa", profile.GetProperty("fullName").GetString());
        Assert.False(profile.TryGetProperty("password", out _));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/auth/me", new { fullName = "" })).StatusCode);
    }
}
