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
public class UserManagementTests : ApiTestBase
{
    [Fact]
    public async Task AdminCanListAndManageUsers()
    {
        var studentId = await Register("STUDENT", "update-target@example.com");
        await Admin();
        var users = await Data(await client.GetAsync("/api/admin/users"));
        Assert.NotEmpty(users.EnumerateArray());
        var updatePayload = new
        {
            fullName = "Người Dùng Mới",
            phoneNumber = "0987654321",
            role = "LANDLORD",
            isEduVerified = true
        };
        var updated = await Data(await client.PutAsJsonAsync($"/api/admin/users/{studentId}", updatePayload));
        Assert.Equal("Người Dùng Mới", updated.GetProperty("fullName").GetString());
        Assert.Equal("LANDLORD", updated.GetProperty("role").GetString());
        Assert.True(updated.GetProperty("isEduVerified").GetBoolean());
    }
}
