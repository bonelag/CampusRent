using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CampusRent.Tests;

public class UserSearchTests : RoomFeatureTestBase
{
    [Fact]
    public async Task AdminSearchesByNameOrEmailAndPasswordResetIsValidated()
    {
        var userId = await Register("STUDENT", "nguyen_van_a@example.com");
        await Admin();

        Assert.Single((await Data(await client.GetAsync("/api/admin/users?q=nguyen_van_a"))).EnumerateArray());
        Assert.Empty((await Data(await client.GetAsync("/api/admin/users?q=khong_co_ai_dau_nhe"))).EnumerateArray());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/admin/users/{userId}/password", new { password = "short" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/admin/users/{Guid.NewGuid()}/password", new { password = "ValidPassword123!" })).StatusCode);
    }
}
