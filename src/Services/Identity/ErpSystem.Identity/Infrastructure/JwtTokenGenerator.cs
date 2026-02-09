using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ErpSystem.Identity.Infrastructure;

public static class JwtTokenGenerator
{
    // Hardcoded for demo - should be in config
    private const string SecretKey = "MySuperSecretKeyThatIsVeryLongAndSecure123!";
    private const string Issuer = "ErpIdentity";
    private const string Audience = "ErpWeb";

    public static string Generate(Guid userId, string username)
    {
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(SecretKey));
        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Name, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        ];

        JwtSecurityToken token = new(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
