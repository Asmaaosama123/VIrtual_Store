using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Vstore.Helpers;
using Vstore.Models;

namespace Vstore.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly JWT _jwt;

        public AuthService(UserManager<User> userManager, IOptions<JWT> jwt)
        {
            _userManager = userManager;
            _jwt = jwt.Value;
        }

        public async Task<AuthModel> RegisterAsync(NewUser newUser)
        {
            // Check if email or username already exists
            if (await _userManager.FindByEmailAsync(newUser.Email) is not null)
                return new AuthModel { Massage = "Email is already registered" };

            if (await _userManager.FindByNameAsync(newUser.UserName) is not null)
                return new AuthModel { Massage = "Username is already taken" };

            // Prepare the user entity
            using var dataStream = new MemoryStream();
            await newUser.Image.CopyToAsync(dataStream);

            var user = new User()
            {
                UserName = newUser.UserName,
                Email = newUser.Email,
                FName = newUser.FName,
                LName = newUser.LName,
                Address = newUser.Address,
                PhoneNumber = newUser.PhoneNumber,
                Image = dataStream.ToArray()
            };

            // Create the user
            var result = await _userManager.CreateAsync(user, newUser.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new AuthModel { Massage = errors };
            }

            // Add user to role
            await _userManager.AddToRoleAsync(user, "User");
            

            // Generate JWT token
            var jwtToken = await CreateJwtToken(user);

            return new AuthModel
            {
                Email = user.Email,
                Expireon = jwtToken.ValidTo,
                IsAuthonticated = true,
                Roles = new List<string> { "User" },
                Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                Username = user.UserName
            };
        }

        private async Task<JwtSecurityToken> CreateJwtToken(User user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var roleClaims = roles.Select(role => new Claim("roles", role)).ToList();

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("uid", user.Id)
            }
            .Union(userClaims)
            .Union(roleClaims);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            return new JwtSecurityToken(
                issuer: _jwt.Issure,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(_jwt.DurationDays),
                signingCredentials: signingCredentials
            );
        }
    }
}
