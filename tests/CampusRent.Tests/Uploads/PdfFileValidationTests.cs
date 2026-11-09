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
public class PdfFileValidationTests : ApiTestBase
{
    [Fact]
    public async Task UploadPdfRejectsNonPdfFiles()
    {
        await Register("STUDENT");
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("not a pdf")), "file", "test.txt");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/uploads/pdf", content)).StatusCode);
    }
}
