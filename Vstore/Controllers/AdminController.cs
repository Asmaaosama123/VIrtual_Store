using Microsoft.AspNetCore.Http;
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
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Vstore.Data;
using Vstore.DTO;
using Vstore.Models;

namespace Vstore.Controllers
{
   
    [Route("api/[controller]")]
   
    [ApiController]
    // [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        public AdminController(UserManager<User> usermanager, IConfiguration configuration, AppDBContext context)
        {
            _userManager = usermanager;
            this.configuration = configuration;
            _context = context;
        }

        private readonly AppDBContext _context;
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
        private string GetAdminEmail()
        {
            return HttpContext.Session.GetString("AdminEmail");
        }

        private bool IsAdminSessionValid(out string adminEmail)
        {
            adminEmail = GetAdminEmail();
            return !string.IsNullOrEmpty(adminEmail);
        }


        [HttpGet("AllOwners")]
        public async Task<ActionResult<IEnumerable<AdminDTO>>> GetOwners()
        {
            // Get all owners and include their requests
            if (!IsAdminSessionValid(out var adminEmail))
            {
                return BadRequest("Admin email is required.");
            }


            //var user = User.Identity?.Name; // Get current user's email from claims
            //if (user == null)
            //{
            //    return Unauthorized("You must be logged in.");
            //}
            var owners = await _context.Owners
                .Include(o => o.Request)
                .Where(o => !o.IsDelete &&
                            (o.Request == null || o.Request.status != Status.Rejected)) // Exclude Rejected status
                .Select(o => new AdminDTO
                {
                    OwnerId = o.Id,
                    FName = o.FName,
                    LName = o.LName,
                    Email = o.Email,
                    UserName = o.UserName,
                    Address = o.Address,
                    PhoneNumber = o.PhoneNumber,

                    // Fields specific to Owner
                    RegistirationDate = o.RegistirationDate,
                    Shop_Name = o.Shop_Name,
                    ImageBase64 = o.Image != null ? Convert.ToBase64String(o.Image) : null,
                    Shop_Description = o.Shop_Description,

                    // Safely accessing related Request information
                    status = o.Request != null ? o.Request.status : Status.Pending,
                    RejectionReason = o.Request != null ? o.Request.RejectionReason : null
                })
                .OrderBy(o => o.status != Status.Pending) // Sort to keep Pending at the top
                .ToListAsync();


            if (owners == null || owners.Count == 0)
            {
                return NotFound("No owners found.");
            }

            return Ok(owners);
        }
        [HttpGet("GetNumofProductsByCategory/{OwnerId}")]
        public async Task<IActionResult> GetNumofProductsByCategory(string OwnerId)
        {

            var products = await _context.Products.Where(p => p.Owner_Id == OwnerId).Select(p => new { p.DefualtImage, p.Product_Name, p.Category_Id }).ToListAsync();
          var numofproduct= products.Count();
            return Ok(new
            {
                numofproduct = numofproduct,
                products = products
            }
              );

        }
        [HttpPost("UpdateRequestStatus")]
        public async Task<IActionResult> UpdateRequestStatus([FromBody] RequestDTO updateRequest)
        {
            // Validate OwnerId
            if (string.IsNullOrEmpty(updateRequest.OwnerId))
            {
                return BadRequest("OwnerId is required.");
            }

            // Validate that the status is a valid enum value
            if (!Enum.IsDefined(typeof(Status), updateRequest.status))
            {
                return BadRequest("Invalid status value.");
            }

            var adminEmail = HttpContext.Session.GetString("AdminEmail");

            if (string.IsNullOrEmpty(adminEmail))
            {
                return BadRequest("Admin email is required.");
            }

            // Find the owner using OwnerId
            var owner = await _context.Owners
                .FirstOrDefaultAsync(o => o.Id == updateRequest.OwnerId);

            if (owner == null)
            {
                return NotFound("Owner not found.");
            }

            // Find the associated request using OwnerId
            var request = await _context.Requests
                .FirstOrDefaultAsync(r => r.OwnerId == owner.Id);

            if (request == null)
            {
                // If no request exists, create a new one
                request = new Request
                {
                    OwnerId = owner.Id,
                    status = updateRequest.status,
                    RejectionReason = updateRequest.RejectionReason
                };
                await _context.Requests.AddAsync(request);
            }
            else
            {
                // Update the existing request
                request.status = updateRequest.status;
                request.RejectionReason = updateRequest.RejectionReason;
            }

            // Only when status is Accepted
            if (request.status == Status.Accepted)
            {
                owner.RegistirationDate = DateTime.UtcNow; // Set registration date to now
            }
            await _context.SaveChangesAsync();

            // Confirmation email logic if status is Accepted
            if (request.status == Status.Accepted)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(owner);
                var confirmationLink = Url.Action("ConfirmEmail", "Account",
                    new { memberkey = owner.Id, tokenresult = HttpUtility.UrlEncode(token) },
                    Request.Scheme);

                await SendEmailAsync(owner.Email, "Confirm your email",
                    $"You are Accepted by the Admin Please confirm your email by clicking this link: {confirmationLink}");

                return Ok(new { Message = "Account confirmed, please check your email." });
            }
            else if (request.status == Status.Rejected)
            {
                string rejectionMessage = $"Unfortunately, your account request has been rejected. Reason: {request.RejectionReason}";
                await SendEmailAsync(owner.Email, "Account Request Rejected", rejectionMessage);
                return Ok(new { Message = "Request status updated to rejected, email sent to the owner." });
            }


            return Ok(new { Message = "Request status updated successfully." });
        }

        [HttpGet("GetRequestByOwnerId/{OwnerId}")]
        public async Task<IActionResult> GetRequestByOwnerId(string OwnerId)
        {
            if (!IsAdminSessionValid(out var adminEmail))
            {
                return BadRequest("Admin email is required.");
            }

            var request = await _context.Requests
                .FirstOrDefaultAsync(r => r.OwnerId == OwnerId);

            if (request == null)
            {
                return NotFound("Request not found.");
            }

            return Ok(new
            {
                status = request.status.ToString(),  // Convert status enum to string
                rejectionReason = request.RejectionReason
            });

        }



        //[HttpDelete("DeleteOwner/{OwnerId}")]

        //public async Task<IActionResult>DeleteOwner(string  OwnerId)
        //{
        //    if (!IsAdminSessionValid(out var adminEmail))
        //    {
        //        return BadRequest("Admin email is required.");
        //    }
        //    if (string.IsNullOrEmpty(OwnerId))
        //    {
        //        return BadRequest("Owner Id is required.");
        //    }
        //    var owner = await _context.Owners.SingleOrDefaultAsync(o => o.Id == OwnerId);
        //    if (owner == null)
        //    { return NotFound($"No genre was found with ID: {OwnerId}"); }
        //    _context.Remove(owner);
        //    _context.SaveChanges();
        //    return Ok(owner);
        //}

        [HttpGet("GetOwnerDetails/{ownerId}")]
        public async Task<IActionResult> GetOwnerDetails(string ownerId)
        {
            if (!IsAdminSessionValid(out var adminEmail))
            {
                return BadRequest("Admin email is required.");
            }

            if (string.IsNullOrEmpty(ownerId))
            {
                return BadRequest("OwnerId is required.");
            }

            // Find the owner using OwnerId
            var owner = await _context.Owners
                .FirstOrDefaultAsync(o => o.Id == ownerId);

            if (owner == null)
            {
                return NotFound("Owner not found.");
            }

            // Find the associated request using OwnerId
            var request = await _context.Requests
        .FirstOrDefaultAsync(r => r.OwnerId == owner.Id);

            // Prepare the response using RequestDTO
            var ownerDetails = new AdminDTO
            {
                OwnerId = owner.Id,
                FName = owner.FName,
                LName = owner.LName,
                Email = owner.Email,
                Address = owner.Address,
                PhoneNumber = owner.PhoneNumber,
                RegistirationDate = owner.RegistirationDate,
                ImageBase64 = owner.Image != null ? Convert.ToBase64String(owner.Image) : null,
                Shop_Name = owner.Shop_Name,
                Shop_Description = owner.Shop_Description,
                status = request?.status ?? Status.Pending,
                RejectionReason = request?.RejectionReason,
                Deletereason= owner.Delete_Reason
            };

            return Ok(ownerDetails);

        }
        [HttpDelete("Delete_Owner/{ownerId}")]
        public async Task<IActionResult> DeleteAsync(string ownerId, [FromBody] string deleteReason)
        {
            var adminEmail = HttpContext.Session.GetString("AdminEmail");
            if (string.IsNullOrEmpty(adminEmail))
            {
                return Unauthorized("Admin authentication is required.");
            }

            if (string.IsNullOrEmpty(deleteReason))
            {
                return BadRequest("Delete reason is required.");
            }

            var owner = await _context.Owners.SingleOrDefaultAsync(o => o.Id == ownerId && !o.IsDelete);
            if (owner == null)
            {
                return NotFound("No owner was found with this ID or it has already been deleted.");
            }

            string DeleteMessage = $"Unfortunately, your account deleted." +
                    $" Reason: {deleteReason}";
            await SendEmailAsync(owner.Email, "Account Deleted", DeleteMessage);
            owner.IsDelete = true;
            owner.EmailConfirmed = false;
            owner.Delete_Reason = deleteReason;
            _context.Owners.Update(owner);
            await _context.SaveChangesAsync();
            return

               Ok(new { Message = "Owner deleted" });


        }

    }
}

