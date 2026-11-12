using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CampusRent.Tests;

public class UserManagementTests : ApiTestBase
{
    [Fact]
    public async Task AdminCanOnlyResetPasswordAndUserListOmitsPrivateContactFields()
    {
        const string email = "password-target@example.com";
        const string oldPassword = "OldPassword123!";
        const string newPassword = "NewPassword456!";
        var registered = await Data(await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = oldPassword,
            fullName = "Password Target",
            phoneNumber = "0987654321"
        }));
        var userId = registered.GetProperty("user").GetProperty("id").GetGuid();

        await Admin();
        var users = await Data(await client.GetAsync("/api/admin/users"));
        var target = users.EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == userId);
        Assert.False(target.TryGetProperty("phoneNumber", out _));
        Assert.False(target.TryGetProperty("address", out _));

        var forbidden = await client.PutAsJsonAsync($"/api/admin/users/{userId}", new
        {
            fullName = "Changed",
            phoneNumber = "0900000000",
            role = "LANDLORD",
            isEduVerified = true
        });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/admin/users/{userId}/password", new { password = newPassword })).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { email, password = oldPassword })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new { email, password = newPassword })).StatusCode);
    }
}
