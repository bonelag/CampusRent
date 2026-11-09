using System.Text.RegularExpressions;

namespace CampusRent.Server.Services;

public sealed class UploadStorage(IWebHostEnvironment environment, IConfiguration config)
{
    public string Root => config["Uploads:RootPath"] ?? Path.Combine(environment.ContentRootPath, "uploads");
    public static bool IsPublicImageUrl(string? url) => url is not null &&
        Regex.IsMatch(url, @"^/uploads/images/[a-f0-9]{32}\.(jpg|png|webp)$", RegexOptions.CultureInvariant);
    public static bool IsSafeName(string? name) => name is not null &&
        Regex.IsMatch(name, @"^[a-f0-9]{32}\.(jpg|png|webp|pdf)$", RegexOptions.CultureInvariant);
    public static string ContentType(string name) => Path.GetExtension(name) switch
    {
        ".jpg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", ".pdf" => "application/pdf",
        _ => "application/octet-stream"
    };

    public static async Task<string?> ImageExtension(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var bytes = new byte[12];
        var count = await stream.ReadAtLeastAsync(bytes, 12, throwOnEndOfStream: false, cancellationToken);
        if (count >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255) return ".jpg";
        if (count >= 8 && bytes.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return ".png";
        if (count == 12 && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" &&
            System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP") return ".webp";
        return null;
    }

    public async Task<string> Save(IFormFile file, string extension, Guid? privateOwner, CancellationToken token)
    {
        var name = Guid.NewGuid().ToString("N") + extension;
        var folder = privateOwner.HasValue ? Path.Combine(Root, "private", privateOwner.Value.ToString("N")) : Path.Combine(Root, "images");
        Directory.CreateDirectory(folder);
        await using var input = file.OpenReadStream();
        await using var output = File.Create(Path.Combine(folder, name));
        await input.CopyToAsync(output, token);
        return privateOwner.HasValue ? $"/api/uploads/files/{privateOwner}/{name}" : $"/uploads/images/{name}";
    }

    public bool IsOwnedPrivateUrl(string? url, Guid owner, bool pdf = false)
    {
        var prefix = $"/api/uploads/files/{owner}/";
        if (url is null || !url.StartsWith(prefix, StringComparison.Ordinal)) return false;
        var name = url[prefix.Length..];
        return IsSafeName(name) && (pdf ? name.EndsWith(".pdf", StringComparison.Ordinal) : !name.EndsWith(".pdf", StringComparison.Ordinal)) &&
            File.Exists(Path.Combine(Root, "private", owner.ToString("N"), name));
    }
}
