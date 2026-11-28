using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CampusRent.Server.Models;
using Microsoft.IdentityModel.Tokens;

namespace CampusRent.Server.Services;

public interface ITokenService
{
    string GenerateToken(User user);
}

public class TokenService(IConfiguration config) : ITokenService
{
    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("credential_version", CredentialVersion(user.Password))
        };

        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!));
        var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string CredentialVersion(string passwordHash)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash));
        return Convert.ToHexString(hashBytes);
    }
}
