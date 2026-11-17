using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CampusRent.Tests;

public class SmtpSettingsTests : ApiTestBase
{
    [Fact]
    public async Task AdminCanConfigureSmtpWithoutExposingPasswordAndStudentsAreForbidden()
    {
        await Register("STUDENT", "student@gmail.com");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/smtp")).StatusCode);

        await Admin();
        var saved = await Data(await client.PutAsJsonAsync("/api/admin/smtp", new
        {
            enabled = true,
            host = "smtp.gmail.com",
            port = 587,
            useSsl = true,
            username = "campusrent.demo@gmail.com",
            password = "abcd efgh ijkl mnop",
            fromEmail = "campusrent.demo@gmail.com",
            fromName = "CampusRent Support"
        }));

        Assert.True(saved.GetProperty("enabled").GetBoolean());
        Assert.True(saved.GetProperty("hasPassword").GetBoolean());
        Assert.False(saved.TryGetProperty("password", out _));

        var fetched = await Data(await client.GetAsync("/api/admin/smtp"));
        Assert.Equal("smtp.gmail.com", fetched.GetProperty("host").GetString());
        Assert.Equal("CampusRent Support", fetched.GetProperty("fromName").GetString());
        Assert.True(fetched.GetProperty("hasPassword").GetBoolean());
        Assert.False(fetched.TryGetProperty("password", out _));
    }

    [Fact]
    public async Task InvalidTestRecipientReturnsBadRequest()
    {
        await Admin();
        var res = await client.PostAsJsonAsync("/api/admin/smtp/test", new { toEmail = "not-an-email" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
