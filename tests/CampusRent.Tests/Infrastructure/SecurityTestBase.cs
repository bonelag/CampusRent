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
public abstract class SecurityTestBase : ApiTestBase
{
    protected async Task<(Guid Id, string Token)> Account(Role role = Role.STUDENT)
    {
        var data = await Data(await client.PostAsJsonAsync("/api/auth/register", new { email = Guid.NewGuid() + "@example.com", password = "TestPassword123!", fullName = "Production test", role = "STUDENT" }));
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

    protected void Use(string token) => client.DefaultRequestHeaders.Authorization = new("Bearer", token);
    protected async Task<string> Upload(bool pdf)
    {
        using var content = new MultipartFormDataContent();
        var bytes = pdf ? System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n%%EOF") : Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jB5kAAAAASUVORK5CYII=");
        content.Add(new ByteArrayContent(bytes), "file", pdf ? "ownership.pdf" : "ownership.png");
        var result = await Data(await client.PostAsync(pdf ? "/api/uploads/pdf" : "/api/uploads/image", content));
        return result.GetProperty("url").GetString()!;
    }
}
