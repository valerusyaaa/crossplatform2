using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class AuthOptions
{
    public static string Issuer => "crossplatform2";
    public static string Audience => "APIclients";
    public static int LifetimeInMinutes => 120;
    public static SecurityKey GetSigningKey(IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(key) || key.Length < 32)
        {
            throw new InvalidOperationException("JWT Key must be at least 32 characters long");
        }
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }

    public static string GenerateToken(string username, string role, IConfiguration configuration)
    {
        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var jwt = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(LifetimeInMinutes),
            signingCredentials: new SigningCredentials(
                GetSigningKey(configuration),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
