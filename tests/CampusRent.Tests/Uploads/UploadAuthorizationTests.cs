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
public class UploadAuthorizationTests : RoomFeatureTestBase
{
    [Fact]
    public async Task UnauthenticatedUserCannotUpload()
    {
        client.DefaultRequestHeaders.Authorization = null;
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(new byte[] { 1, 2 }), "file", "test.pdf");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/uploads/pdf", content)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/uploads/image", content)).StatusCode);
    }
}
