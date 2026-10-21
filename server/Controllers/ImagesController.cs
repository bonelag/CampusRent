using CampusRent.Server.DTOs.Common;
using CampusRent.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CampusRent.Server.Controllers;
[ApiController, Route("api/images"), Authorize(Roles = "LANDLORD,ADMIN")]
public class ImagesController(ICloudinaryService images) : ControllerBase
{
    [HttpPost, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file.Length is <= 0 or > 5 * 1024 * 1024) return BadRequest(ApiResponse<object>.Fail("Ảnh phải nhỏ hơn 5 MB."));
        await using var stream = file.OpenReadStream();
        var bytes = new byte[12]; var count = await stream.ReadAsync(bytes);
        var jpeg = count >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
        var png = count >= 8 && bytes.Take(8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10});
        var webp = count == 12 && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP";
        if (!jpeg && !png && !webp) return BadRequest(ApiResponse<object>.Fail("Chỉ hỗ trợ ảnh JPG, PNG hoặc WebP."));
        var result = await images.UploadImageAsync(file);
        return result.Error == null ? Ok(ApiResponse<object>.Ok(new { url = result.Url })) : StatusCode(503, ApiResponse<object>.Fail(result.Error));
    }
}
