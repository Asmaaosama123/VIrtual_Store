using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net;
using System.Web;
using Microsoft.Extensions.Configuration;
using Vstore.Data;
using Vstore.DTO;
using Vstore.Models;
using Vstore.Services;
using Vstore.Helpers;
using Microsoft.Extensions.Options;

namespace Vstore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {

        private new List<string> _allowedextension = new List<string> { ".jpg", ".jpeg", ".tif", ".png" };
        private long postersize = 1048567;
        private IOptions<JWT> _jwt;
        private object _logger;
        private readonly IAuthService authService;
        public AccountController(UserManager<User> usermanager, IConfiguration configuration, AppDBContext context, IAuthService authService)
        {
            _userManager = usermanager;
            this.configuration = configuration;
            _context = context;
            _authService = authService;
        }

        private readonly AppDBContext _context;
        private readonly IAuthService _authService;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration configuration;
        private async Task SendEmailAsync(string email, string subject, string message)
        {
            using var client = new SmtpClient(configuration["EmailSettings:SmtpServer"], int.Parse(configuration["EmailSettings:SmtpPort"]))
            {
                Credentials = new NetworkCredential(configuration["EmailSettings:SmtpUser"], configuration["EmailSettings:SmtpPass"]),
                EnableSsl = true
            };


            var mailMessage = new MailMessage
            {
                From = new MailAddress(configuration["EmailSettings:SmtpUser"]),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            await client.SendMailAsync(mailMessage);

        }
        [HttpPost("User_Register")]
        public async Task<IActionResult> Register([FromForm] NewUser user)
        {
            if (user.Password != user.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");
                return BadRequest(ModelState);
            }
            if (!_allowedextension.Contains(Path.GetExtension(user.Image.FileName).ToLower()) || user.Image.Length > postersize)
                return BadRequest("Only .tif, .jpg, .jpeg images are allowed, or file size exceeds the limit.");
            using var dataStream = new MemoryStream();
            await user.Image.CopyToAsync(dataStream);
            if (ModelState.IsValid)
            {

                User appuser = new()
                {
                    UserName = user.UserName,
                    Email = user.Email,
                    FName = user.FName,
                    LName = user.LName,
                    Address = user.Address,
                    PhoneNumber = user.PhoneNumber,
                    Image = dataStream.ToArray(),
                     Roles = Roles.User,


                };
               
                var result = await _userManager.CreateAsync(appuser, user.Password);
                if (result.Succeeded)
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(appuser);
                    var confirmationLink = Url.Action("ConfirmEmail", "Account", new { memberkey = appuser.Id, tokenresult = HttpUtility.UrlEncode(token) }, Request.Scheme);


                    await SendEmailAsync(user.Email, "Confirm your email", $"Please confirm your email by clicking on the link: {confirmationLink}");
                    var cart = new Cart
                    {
                        UserId = appuser.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();

                    return Ok(new{
                        message = "User Registered. Please check your email to confirm your account.",
                        id= appuser.Id,
                            });


                   // return Ok("Register Done Successfully");
                }
                else
                {
                    foreach (var item in result.Errors)
                    {
                        ModelState.AddModelError("", item.Description);
                    }

                }
            }


            return BadRequest(ModelState);
           
        
        
        
        }
    





    [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string memberkey, string tokenresult)
        {
            var user = await _userManager.FindByIdAsync(memberkey);
            if (user == null) return NotFound("User not found.");
            if (user.EmailConfirmed)
            {
                return Ok("the email has been confirmed");
            }
            //var result = await _userManager.ConfirmEmailAsync(user,tokenresult);
            var decodedToken = HttpUtility.UrlDecode(tokenresult);
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (result.Succeeded) return Ok("Email confirmed successfully.");

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest($"Email confirmation failed. Errors: {errors}");
        }
        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromForm] string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                return BadRequest("User not found or email not confirmed.");
            }

            // Generate reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = HttpUtility.UrlEncode(token);

            // Dynamically generate the reset password link based on current request
            var callbackUrl = Url.Action(
                "ResetPassword",
                "Account",
                new { email = email, token = encodedToken },
                protocol: Request.Scheme
            //host: Request.Host.ToString()
            );

            //// Send the reset email
            //await SendEmailAsync(user.Email, "Reset Password",$"To Reset Password enter this code: {callbackUrl}");
            string Code = $"The Code of reset Password is : {encodedToken}";
            await SendEmailAsync(user.Email, "Reset Password", Code);

            // Optionally return the link in the response for testing
            return Ok(new
            {
                Message = "Password reset link has been sent to your email.",
                ResetLink = callbackUrl,  // for testing, remove in production
                Token = encodedToken       // for testing, remove in production
            });
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromForm] ResetPasswordDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Find the user by email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest("User not found.");

            // Decode the token to handle URL-encoded values
            var decodedToken = HttpUtility.UrlDecode(model.Token);

            // Attempt to reset the password with the decoded token
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);

            if (result.Succeeded)
                return Ok("Password has been reset successfully.");

            // Handle errors if password reset fails
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return BadRequest(ModelState);
        }



        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromForm] Login dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return BadRequest("Email is required");
            }
            if (!user.EmailConfirmed)
            {
                return BadRequest("Account not confirmed. Please check your email to confirm your account.");
            }

           
            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                return Unauthorized("Invalid password");
            }
            else
            {
                var Claims = new List<Claim>
             {
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
};
                var roles=await _userManager.GetRolesAsync(user);
                foreach (var role in roles)
                {
                    Claims.Add(new Claim(ClaimTypes.Role,role.ToString()));
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Key"]));
                var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var token= new JwtSecurityToken(
                issuer: configuration["JWT:Issure"],
                audience: configuration["JWT:Audience"],
                claims: Claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: signingCredentials
            );
                var _token = new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    Expireon = token.ValidTo


                };

                if (dto.Email == "tshahd733@gmail.com")
                {
                    // var response = await AssignAdminRole("admin@example.com");
                    //   await _userManager.AddToRoleAsync(user, "Admin");

                    HttpContext.Session.SetString("AdminEmail", dto.Email);
                    HttpContext.Session.SetString("UserRole", "Admin");
                    return Ok(new
                    {
                        Role = "Admin",
                        Message = "Admin verified",
                        Token = _token
                    });
                }
                var owner = await _context.Owners.FirstOrDefaultAsync(o => o.Id == user.Id);
                if (owner != null)
                {
                    var request = await _context.Requests.FirstOrDefaultAsync(r => r.OwnerId == owner.Id);

                    if (request != null && request.status == Status.Rejected)
                    {
                        return BadRequest("Your request has been rejected. You cannot log in.");
                    }
                    if (owner.IsDelete)
                    {
                        return BadRequest("Not Found");
                    }
                    //  await _userManager.AddToRoleAsync(user, "Owner");

                    HttpContext.Session.SetString("OwnerId", owner.Id);
                    HttpContext.Session.SetString("UserRole", Roles.Owner.ToString());
                //    Console.WriteLine($"Claims: {string.Join(", ", Claims.Select(c => $"{c.Type}: {c.Value}"))}");
             //  return Ok(_token + owner.Id);

                    return Ok(new
                    {
                        Role = "Owner",
                        Message = "Owner verified",
                        Token = _token,
                        id= owner.Id

                    });
                }

                var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == user.Id);
                int? cartId = cart?.CartId;

                HttpContext.Session.SetString("UserRole", "User");
            //    await _userManager.AddToRoleAsync(user, "User");

                return Ok(new
                {
                    Role = "User",
                    Message = "User verified",
                    Token = _token,
                    id=user.Id,
                    CartId = cartId
                });
            }

           

           

          
        }
        [HttpPost("ChangePassword/{UserId}")]
        public async Task<IActionResult> ChangePassword([FromForm] ChangePasswordDTO request,string UserId)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid request.");

           // var userId = HttpContext.Session.GetString("UserId");

            var user = await _userManager.Users
                .Where(u => u.Id == UserId && u.EmailConfirmed)
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound("User not found or email not confirmed.");

            var passwordHasher = new PasswordHasher<User>();
            var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);

            if (verificationResult == PasswordVerificationResult.Failed)
                return BadRequest("Current password is incorrect.");

            var newPasswordHash = passwordHasher.HashPassword(user, request.NewPassword);

            user.PasswordHash = newPasswordHash;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new { Errors = errors });
            }

            return Ok("Password changed successfully.");
        }


        [HttpPost("Logout")]
        public IActionResult Logout()
        {
            // Clear session or cookies
            HttpContext.Session.Clear(); // Clears the session

            // Optionally, if you're using cookies for authentication, remove the cookies as well
            // Response.Cookies.Delete("YourAuthCookieName"); 

            return Ok(new { message = "Logged out successfully" });
        }
    }
}
