using dj_api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace dj_api.Authentication
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;

       
        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateJwtToken(User user)
        {
           
            var jwtSettings = _configuration.GetSection("JWTSecrets");
            var secretKey = Encoding.UTF8.GetBytes(jwtSettings["secretKey"]!);
            var issuer = jwtSettings["issuer"];
            var audience = jwtSettings["audience"];
            var expiresInHours = int.Parse(jwtSettings["expires"]!); 

         
            var securityKey = new SymmetricSecurityKey(secretKey);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

         
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.ObjectId),
                new Claim(ClaimTypes.Name, user.Name),
            
            };

          
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.Now.AddHours(expiresInHours), 
                signingCredentials: credentials
            );

         
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
