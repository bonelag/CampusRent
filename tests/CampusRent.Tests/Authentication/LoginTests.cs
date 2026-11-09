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
public class LoginTests : ApiTestBase
{
    [Fact]
    public async Task InvalidPasswordDoesNotAuthenticate()
    {
        await Register(email: "student@example.com");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { email = "student@example.com", password = "wrong" })).StatusCode);
    }

    [Fact]
    public async Task LoginAttemptsAreRateLimited()
    {
        HttpResponseMessage? response = null;
        for (var i = 0; i < 21; i++)
            response = await client.PostAsJsonAsync("/api/auth/login", new { email = "none@example.com", password = "wrong" });
        Assert.Equal(HttpStatusCode.TooManyRequests, response!.StatusCode);
    }
}
