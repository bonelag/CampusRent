using CampusRent.Server.DTOs.Common;
using CampusRent.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusRent.Server.Controllers;

[ApiController, Route("api/uploads"), Authorize]
public class UploadsController(ICloudinaryService images, IWebHostEnvironment env) : ControllerBase
{
    [HttpPost("image"), RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length <= 0 || file.Length > 10 * 1024 * 1024)
            return BadRequest(ApiResponse<object>.Fail("Ảnh phải có dung lượng từ 1 B đến 10 MB."));

        await using var stream = file.OpenReadStream();
        var bytes = new byte[12];
        var count = await stream.ReadAsync(bytes);
        var jpeg = count >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
        var png = count >= 8 && bytes.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var webp = count == 12 && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP";

        if (!jpeg && !png && !webp)
            return BadRequest(ApiResponse<object>.Fail("Chỉ hỗ trợ file ảnh JPG, PNG hoặc WebP."));

        // Try Cloudinary first if configured
        var cloudinaryResult = await images.UploadImageAsync(file);
        if (cloudinaryResult.Url != null)
            return Ok(ApiResponse<object>.Ok(new { url = cloudinaryResult.Url }));

        // Fallback to local storage
        var ext = jpeg ? ".jpg" : png ? ".png" : ".webp";
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var folder = Path.Combine(env.ContentRootPath, "uploads", "images");
        Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, fileName);

        stream.Position = 0;
        await using var output = System.IO.File.Create(filePath);
        await stream.CopyToAsync(output);

        var url = $"/uploads/images/{fileName}";
        return Ok(ApiResponse<object>.Ok(new { url }));
    }

    [HttpPost("pdf"), RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> UploadPdf(IFormFile file)
    {
        if (file == null || file.Length <= 0 || file.Length > 15 * 1024 * 1024)
            return BadRequest(ApiResponse<object>.Fail("File PDF phải có dung lượng dưới 15 MB."));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".pdf")
            return BadRequest(ApiResponse<object>.Fail("Chỉ cho phép tải lên file định dạng PDF (.pdf)."));

        await using var stream = file.OpenReadStream();
        var header = new byte[5];
        var count = await stream.ReadAsync(header);
        // "%PDF-" in ASCII is [0x25, 0x50, 0x44, 0x46, 0x2D]
        if (count < 5 || System.Text.Encoding.ASCII.GetString(header) != "%PDF-")
            return BadRequest(ApiResponse<object>.Fail("File không phải định dạng PDF hợp lệ."));

        var safeName = Path.GetFileNameWithoutExtension(file.FileName);
        safeName = string.Concat(safeName.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
        if (safeName.Length > 50) safeName = safeName.Substring(0, 50);

        var fileName = $"{Guid.NewGuid():N}_{safeName}.pdf";
        var folder = Path.Combine(env.ContentRootPath, "uploads", "documents");
        Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, fileName);

        stream.Position = 0;
        await using var output = System.IO.File.Create(filePath);
        await stream.CopyToAsync(output);

        var url = $"/uploads/documents/{fileName}";
        return Ok(ApiResponse<object>.Ok(new { url, fileName = file.FileName, size = file.Length }));
    }
}
