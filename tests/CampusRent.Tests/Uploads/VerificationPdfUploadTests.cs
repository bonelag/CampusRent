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
public class VerificationPdfUploadTests : RoomFeatureTestBase
{
    [Fact]
    public async Task UploadPdfSucceedsWithValidPdfHeader()
    {
        await Register("STUDENT");
        using var content = new MultipartFormDataContent();
        var pdfBytes = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\n<<>>\nendobj\ntrailer\n<<>>\n%%EOF");
        content.Add(new ByteArrayContent(pdfBytes), "file", "giay_to_nha_dat.pdf");
        var response = await client.PostAsync("/api/uploads/pdf", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await Data(response);
        Assert.EndsWith(".pdf", data.GetProperty("url").GetString());
        Assert.Equal("giay_to_nha_dat.pdf", data.GetProperty("fileName").GetString());
    }

    [Fact]
    public async Task UploadPdfRejectsNonPdfExtensions()
    {
        await Register("STUDENT");
        using var content = new MultipartFormDataContent();
        var pdfBytes = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4 fake header");
        content.Add(new ByteArrayContent(pdfBytes), "file", "malicious.exe");
        var response = await client.PostAsync("/api/uploads/pdf", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
