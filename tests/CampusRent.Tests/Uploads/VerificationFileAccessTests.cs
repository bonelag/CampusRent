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
public class VerificationFileAccessTests : SecurityTestBase
{
    [Fact]
    public async Task VerificationFilesArePrivateAndAttachmentsCannotReferenceAnotherOwner()
    {
        var owner = await Account();
        var image = await Upload(false);
        var pdf = await Upload(true);
        Assert.StartsWith($"/api/uploads/files/{owner.Id}/", image);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(pdf)).StatusCode);
        var response = await client.GetAsync(image);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(image)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(pdf)).StatusCode);
        await Account();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(image)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(pdf)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/landlord-requests", new { title = "Wrong owner", reason = "Cannot borrow files", imageUrls = new[] { image }, pdfUrl = pdf })).StatusCode);
        await Account(Role.ADMIN);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(image)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(pdf)).StatusCode);
        Use(owner.Token);
        await Data(await client.PostAsJsonAsync("/api/landlord-requests", new { title = "Own documents", reason = "Correct attachments", imageUrls = new[] { image }, pdfUrl = pdf }));
    }
}
