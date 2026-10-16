using CampusRent.Server.Models;

namespace CampusRent.Server.Services;

public interface ITokenService
{
    string GenerateToken(User user);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(User user)
    {
        // TODO: Thành viên nhóm triển khai:
        // Đọc Jwt:Secret, Jwt:Issuer, Jwt:Audience từ configuration
        // Tạo JWT Token chứa các claims: NameIdentifier (UserId), Email, Name, Role, isEduVerified
        return string.Empty;
    }
}
