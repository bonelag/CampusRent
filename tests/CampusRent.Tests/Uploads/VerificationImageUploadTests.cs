using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CampusRent.Server.Data;
using CampusRent.Server.Models;
using CampusRent.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusRent.Tests;
public class VerificationImageUploadTests : RoomFeatureTestBase
{
    [Fact]
    public async Task VerificationImageIsStoredUnderAuthenticatedFileEndpoint()
    {
        await Register("LANDLORD");
        using var content = new MultipartFormDataContent();
        var pngHeader = new byte[]
        {
            137,
            80,
            78,
            71,
            13,
            10,
            26,
            10,
            0,
            0,
            0,
            0
        };
        content.Add(new ByteArrayContent(pngHeader), "file", "room_test.png");
        var response = await client.PostAsync("/api/uploads/image", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await Data(response);
        var url = data.GetProperty("url").GetString();
        Assert.NotNull(url);
        Assert.StartsWith("/api/uploads/files/", url);
        Assert.EndsWith(".png", url);
    }

    [Fact]
    public async Task UploadImageRejectsCorruptBytes()
    {
        await Register("LANDLORD");
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 }), "file", "corrupt.jpg");
        var response = await client.PostAsync("/api/uploads/image", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
