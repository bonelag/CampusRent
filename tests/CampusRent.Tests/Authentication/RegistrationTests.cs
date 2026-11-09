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
public class RegistrationTests : ApiTestBase
{
    [Fact]
    public async Task RegistrationCannotEscalateToAdmin() => Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/register", new { email = "hacker@example.com", password = "TestPass123!", fullName = "Hacker", role = "ADMIN" })).StatusCode);
    [Fact]
    public async Task InvalidRegistrationIsRejected() => Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/register", new { email = "invalid", password = "123", fullName = "" })).StatusCode);
    [Fact]
    public async Task AcademicEmailIsNotAutomaticallyVerified()
    {
        await Register(email: "student@hust.edu.vn");
        var user = await Data(await client.GetAsync("/api/auth/me"));
        Assert.False(user.GetProperty("isEduVerified").GetBoolean());
    }

    [Fact]
    public async Task EmailNormalizationAndDuplicateCheck()
    {
        await Register(email: "Student@Example.com");
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/register", new { email = "student@example.com", password = "TestPass123!", fullName = "Other" })).StatusCode);
        var data = await Data(await client.PostAsJsonAsync("/api/auth/login", new { email = "STUDENT@example.com", password = "TestPass123!" }));
        Assert.Equal("student@example.com", data.GetProperty("user").GetProperty("email").GetString());
    }

    [Fact]
    public async Task UnicodePasswordCannotExceedBcryptByteLimit()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/register", new { email = "test@example.com", password = new string ('ệ', 30), fullName = "Test" })).StatusCode);
    }
}
