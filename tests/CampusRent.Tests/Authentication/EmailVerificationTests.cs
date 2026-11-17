using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CampusRent.Tests;

public class EmailVerificationTests : ApiTestBase
{
    [Fact]
    public async Task UnverifiedLandlordCannotPostUntilEmailOtpVerified()
    {
        await Register("LANDLORD", "newlandlord@gmail.com", verifyEmail: false);

        var blocked = await client.PostAsJsonAsync("/api/rooms", RoomPayload());
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);

        var sendData = await Data(await client.PostAsync("/api/auth/send-verification", null));
        var previewCode = sendData.GetProperty("previewCode").GetString();
        Assert.False(string.IsNullOrWhiteSpace(previewCode));

        var wrong = await client.PostAsJsonAsync("/api/auth/verify-email", new { code = "000000" });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);

        var profile = await Data(await client.PostAsJsonAsync("/api/auth/verify-email", new { code = previewCode }));
        Assert.True(profile.GetProperty("isEmailVerified").GetBoolean());

        var created = await client.PostAsJsonAsync("/api/rooms", RoomPayload());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    }

    [Fact]
    public async Task VerifyingEduEmailAutomaticallyActivatesEduBadge()
    {
        await Register("STUDENT", "sv@hust.edu.vn", verifyEmail: false);

        var sendData = await Data(await client.PostAsync("/api/auth/send-verification", null));
        var code = sendData.GetProperty("previewCode").GetString();
        var profile = await Data(await client.PostAsJsonAsync("/api/auth/verify-email", new { code }));

        Assert.True(profile.GetProperty("isEmailVerified").GetBoolean());
        Assert.True(profile.GetProperty("isEduVerified").GetBoolean());
    }
}
