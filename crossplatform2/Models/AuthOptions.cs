using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace crossplatform2.Models
{
    public class AuthOptions
    {
        public static string Issuer => "crossplatform2";
        public static string Audience => "APIclients";
        public static int LifetimeInMinutes => 120;
        public static SecurityKey SigningKey => new SymmetricSecurityKey(
            Encoding.ASCII.GetBytes("superSecretKeyMustBeLoooooongForCrossPlatform2App"));

        public static string GenerateToken(string username, string role)
        {
            var now = DateTime.UtcNow;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };

            var jwt = new JwtSecurityToken(
                issuer: Issuer,
                audience: Audience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(LifetimeInMinutes),
                signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}