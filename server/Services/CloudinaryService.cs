using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
namespace CampusRent.Server.Services;
public interface ICloudinaryService { Task<(string? Url, string? PublicId, string? Error)> UploadImageAsync(IFormFile file, string folder = "campusrent/rooms"); }
public class CloudinaryService(IConfiguration config, ILogger<CloudinaryService> logger) : ICloudinaryService
{
    public async Task<(string? Url, string? PublicId, string? Error)> UploadImageAsync(IFormFile file, string folder = "campusrent/rooms")
    {
        if (string.IsNullOrWhiteSpace(config["Cloudinary:ApiSecret"])) return (null, null, "Chưa cấu hình dịch vụ ảnh. Bạn có thể sử dụng đường dẫn ảnh HTTPS.");
        try
        {
            var cloudinary = new Cloudinary(new Account(config["Cloudinary:CloudName"], config["Cloudinary:ApiKey"], config["Cloudinary:ApiSecret"]));
            await using var stream = file.OpenReadStream();
            var result = await cloudinary.UploadAsync(new ImageUploadParams { File = new FileDescription(file.FileName, stream), Folder = folder, Transformation = new Transformation().Width(1600).Crop("limit").Quality("auto") });
            return result.Error == null ? (result.SecureUrl.ToString(), result.PublicId, null) : (null, null, "Không thể tải ảnh lên.");
        }
        catch (Exception ex) { logger.LogError(ex, "Image upload failed"); return (null, null, "Không thể tải ảnh lên."); }
    }
}
