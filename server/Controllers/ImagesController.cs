using CampusRent.Server.Data;
using CampusRent.Server.DTOs.Common;
using CampusRent.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CampusRent.Server.Controllers;

[ApiController, Route("api/images"), Authorize(Roles = "LANDLORD,ADMIN")]
public class ImagesController(ICloudinaryService images, UploadStorage uploads, CampusRentDbContext db) : ControllerBase
{
    [HttpPost, RequestSizeLimit(6 * 1024 * 1024), EnableRateLimiting("auth")]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken token)
    {
        if (file == null || file.Length is <= 0 or > 5 * 1024 * 1024)
            return BadRequest(ApiResponse<object>.Fail("Ảnh phải nhỏ hơn 5 MB."));
        var extension = await UploadStorage.ImageExtension(file, token);
        if (extension == null) return BadRequest(ApiResponse<object>.Fail("Chỉ hỗ trợ ảnh JPG, PNG hoặc WebP."));
        var remote = await images.UploadImageAsync(file);
        var url = remote.Url ?? await uploads.Save(file, extension, null, token);
        return Ok(ApiResponse<object>.Ok(new { url }));
    }

    [AllowAnonymous, HttpGet("/uploads/images/{fileName}")]
    public async Task<IActionResult> PublicImage(string fileName)
    {
        var url = "/uploads/images/" + fileName;
        if (!UploadStorage.IsPublicImageUrl(url)) return NotFound();
        // Legacy verification images shared this folder. Keep those restricted to their owner/admin.
        if (!await db.RoomImages.AnyAsync(i => i.Url == url))
        {
            var references = db.LandlordRequests.FromSqlInterpolated($"SELECT * FROM landlord_requests WHERE image_urls LIKE {"%\"" + url + "\"%"}");
            if (await references.AnyAsync())
            {
                if (User.Identity?.IsAuthenticated != true) return Unauthorized();
                if (!User.IsInRole("ADMIN") && !await references.AnyAsync(r => r.UserId == User.UserId())) return Forbid();
                Response.Headers.CacheControl = "private,no-store";
            }
        }
        var path = Path.Combine(uploads.Root, "images", fileName);
        return System.IO.File.Exists(path) ? PhysicalFile(path, UploadStorage.ContentType(fileName)) : NotFound();
    }
}
