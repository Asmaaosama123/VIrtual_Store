using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vstore.Models;
using Vstore.Services;

namespace Vstore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavListController : ControllerBase
    {
        private readonly NotificationService _notificationService;
        private readonly AppDBContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;

        public FavListController(UserManager<User> userManager, IConfiguration configuration, AppDBContext context, NotificationService notificationService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _context = context;
            _notificationService = notificationService;
        }
        [HttpPost("AddToFavList/{ownerId}/{userId}")]
        public async Task<IActionResult> AddToFavList(string ownerId, string userId)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(ownerId) || string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { success = false, message = "OwnerId and UserId are required." });
            }

            // Check if the user exists
            var userExists = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId); // Assuming Users is your user table
            if (userExists == null)
            {
                return NotFound(new { success = false, message = "User not found." });
            }

            // Check if the owner/shop exists
            var owner = await _context.Owners.FirstOrDefaultAsync(o => o.Id == ownerId); // Assuming Owners is your owner table
            if (owner == null)
            {
                return NotFound(new { success = false, message = "Owner not found." });
            }

            // Retrieve the user's favorite list or create a new one
            var favList = await _context.FavLists
                .Include(fl => fl.FavListShops)
                .FirstOrDefaultAsync(fl => fl.User_Id == userId);

            if (favList == null)
            {
                // Create a new favorite list for the user if none exists
                favList = new FavList
                {
                    User_Id = userId,
                    FavListShops = new List<FavListShop>()
                };

                _context.FavLists.Add(favList);
                await _context.SaveChangesAsync(); // Save changes to generate FavList_Id
            }

            // Ensure the same shop is not added multiple times in the same favorite list
            if (favList.FavListShops.Any(s => s.Owner_Id == ownerId))
            {
                return Conflict(new { success = false, message = "Shop is already in your favorite list." });
            }

            // Add the shop to the user's favorite list
            var favListShop = new FavListShop
            {
                FavList_Id = favList.FavList_Id,
                Owner_Id = ownerId
            };
            favList.FavListShops.Add(favListShop);

            // Save changes to the database
            await _context.SaveChangesAsync();

            // Send notification to the owner
            try
            {
                var notification = new Notification
                {
                    Title = "New Favorite!",
                    Body = $"User '{userExists.FName} {userExists.LName}' has added your shop '{owner.Shop_Name}' to their favorite list.",
                    DateTime = DateTime.Now,
                    isread = false,
                    User_Id = owner.Id,
                    Notification_Message = $"User '{userExists.FName}' marked your shop as a favorite.",
                    Image = userExists.Image // Optional: Include user's profile picture if available
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Notify the owner via SignalR
                await _notificationService.SendNotificationWithImage(
                    owner.Id,
                    notification.Title,
                    notification.Body,
                    notification.Image != null ? Convert.ToBase64String(notification.Image) : null
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending notification: {ex.Message}");
            }

            return Ok(new { success = true, message = "Shop added to your favorite list successfully." });
        }
        [HttpDelete("DeleteFavList/{userId}/{ownerId}")]
        public async Task<IActionResult> DeleteFavList(string userId, string ownerId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(ownerId))
                return BadRequest("Both userId and ownerId are required.");

            // Get the user's FavList
            var favList = await _context.FavLists
                .Include(f => f.FavListShops)
                .FirstOrDefaultAsync(f => f.User_Id == userId);

            if (favList == null)
                return NotFound("User has no favorite list.");

            var favListEntry = favList.FavListShops.FirstOrDefault(fs => fs.Owner_Id == ownerId);

            if (favListEntry == null)
                return NotFound("Favorite shop entry not found.");

            // Remove the shop from the list
            _context.FavListShops.Remove(favListEntry);
            await _context.SaveChangesAsync();

            // If no remaining shops, delete the whole FavList (NOT the user!)
            if (!favList.FavListShops.Any())
            {
                _context.FavLists.Remove(favList);
                await _context.SaveChangesAsync();
            }

            return Ok("Favorite shop entry deleted successfully.");
        }

    }
}
