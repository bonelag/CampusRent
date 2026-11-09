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
public class ListingImageUploadTests : ApiTestBase
{
    [Fact]
    public async Task UploadRejectsDisguisedFiles()
    {
        await Register("LANDLORD");
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("<script>alert(1)</script>")), "file", "fake.jpg");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/images", content)).StatusCode);
    }

    [Fact]
    public async Task ValidListingImageWithoutCloudinaryUsesPersistentLocalStorage()
    {
        await Register("LANDLORD");
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0 }), "file", "image.png");
        var image = await Data(await client.PostAsync("/api/images", content));
        Assert.StartsWith("/uploads/images/", image.GetProperty("url").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(image.GetProperty("url").GetString())).StatusCode);
    }
}
