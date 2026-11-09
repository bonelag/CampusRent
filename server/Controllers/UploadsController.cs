using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CampusRent.Server.Controllers;

[ApiController, Route("api/uploads"), Authorize]
public class UploadsController(UploadStorage uploads, CampusRentDbContext db) : ControllerBase
{
    [HttpPost("image"), RequestSizeLimit(11 * 1024 * 1024), EnableRateLimiting("auth")]
    public async Task<IActionResult> UploadImage(IFormFile? file, CancellationToken token)
    {
        if (file == null || file.Length is <= 0 or > 10 * 1024 * 1024)
            return BadRequest(ApiResponse<object>.Fail("Ảnh phải có dung lượng từ 1 B đến 10 MB."));
        var extension = await UploadStorage.ImageExtension(file, token);
        if (extension == null) return BadRequest(ApiResponse<object>.Fail("Chỉ hỗ trợ file ảnh JPG, PNG hoặc WebP."));
        var url = await uploads.Save(file, extension, User.UserId(), token);
        return Ok(ApiResponse<object>.Ok(new { url }));
    }

    [HttpPost("pdf"), RequestSizeLimit(16 * 1024 * 1024), EnableRateLimiting("auth")]
    public async Task<IActionResult> UploadPdf(IFormFile? file, CancellationToken token)
    {
        if (file == null || file.Length is <= 0 or > 15 * 1024 * 1024)
            return BadRequest(ApiResponse<object>.Fail("File PDF phải có dung lượng dưới 15 MB."));
        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<object>.Fail("Chỉ cho phép tải lên file định dạng PDF (.pdf)."));
        await using var stream = file.OpenReadStream();
        var header = new byte[5];
        var count = await stream.ReadAtLeastAsync(header, 5, throwOnEndOfStream: false, token);
        if (count != 5 || System.Text.Encoding.ASCII.GetString(header) != "%PDF-")
            return BadRequest(ApiResponse<object>.Fail("File không phải định dạng PDF hợp lệ."));
        var url = await uploads.Save(file, ".pdf", User.UserId(), token);
        return Ok(ApiResponse<object>.Ok(new { url, fileName = Path.GetFileName(file.FileName), size = file.Length }));
    }

    [HttpGet("files/{ownerId:guid}/{fileName}")]
    public IActionResult PrivateFile(Guid ownerId, string fileName)
    {
        if (!UploadStorage.IsSafeName(fileName)) return NotFound();
        if (ownerId != User.UserId() && !User.IsInRole("ADMIN")) return Forbid();
        var path = Path.Combine(uploads.Root, "private", ownerId.ToString("N"), fileName);
        Response.Headers.CacheControl = "private,no-store";
        return System.IO.File.Exists(path) ? PhysicalFile(path, UploadStorage.ContentType(fileName), enableRangeProcessing: true) : NotFound();
    }

    [HttpGet("/uploads/documents/{fileName}")]
    public async Task<IActionResult> LegacyDocument(string fileName)
    {
        if (Path.GetFileName(fileName) != fileName || !fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) return NotFound();
        var url = "/uploads/documents/" + fileName;
        if (!User.IsInRole("ADMIN") && !await db.LandlordRequests.AnyAsync(r => r.UserId == User.UserId() && r.PdfUrl == url)) return Forbid();
        var path = Path.Combine(uploads.Root, "documents", fileName);
        Response.Headers.CacheControl = "private,no-store";
        return System.IO.File.Exists(path) ? PhysicalFile(path, "application/pdf", enableRangeProcessing: true) : NotFound();
    }
}
