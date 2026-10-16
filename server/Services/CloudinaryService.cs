namespace CampusRent.Server.Services;

public interface ICloudinaryService
{
    Task<(string? Url, string? PublicId, string? Error)> UploadImageAsync(IFormFile file, string folder = "campusrent/rooms");
}

public class CloudinaryService : ICloudinaryService
{
    public CloudinaryService(IConfiguration config, ILogger<CloudinaryService> logger)
    {
    }

    public async Task<(string? Url, string? PublicId, string? Error)> UploadImageAsync(IFormFile file, string folder = "campusrent/rooms")
    {
        // TODO: Thành viên nhóm triển khai:
        // Cấu hình CloudinaryDotNet Account từ Cloudinary:CloudName, ApiKey, ApiSecret
        // Đọc stream từ IFormFile và upload lên Cloudinary với folder chỉ định
        // Trả về (SecureUrl, PublicId, Error)
        await Task.CompletedTask;
        return (null, null, "Chưa triển khai CloudinaryService");
    }
}
