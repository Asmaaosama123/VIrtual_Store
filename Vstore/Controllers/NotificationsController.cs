using Microsoft.AspNetCore.Mvc;
using Vstore.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using System.Net.Mail;
using System.Net;
using Vstore.Services;

namespace Vstore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDBContext _context;
        private readonly NotificationService _notificationService;


        public NotificationsController(AppDBContext context, NotificationService notificationService)
        {
            _notificationService = notificationService;

            _context = context;
        }



        [HttpGet("getnotifications/signalr/{userId}")]
        public async Task<IActionResult> GetNotificationsViaSignalR(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("User ID cannot be null or empty.");

            try
            {
                // Fetch notifications for the user
                var notifications = await _context.Notifications
                    .Where(n => n.User_Id == userId)
                    .OrderByDescending(n => n.Notification_Id)
                    .ToListAsync();

                if (notifications == null || !notifications.Any())
                    return Ok(new { Message = "No notifications found for this user." });

                // Notify the user via SignalR using NotificationService
                await _notificationService.SendNotificationsToUser(userId);

                return Ok(new
                {
                    Message = "Notifications sent via SignalR.",
                    Notifications = notifications
                });
            }
            catch (Exception ex)
            {
                // Log detailed error
                Console.WriteLine($"Error fetching notifications: {ex.Message}");
                return StatusCode(500, new
                {
                    Error = "An error occurred while retrieving notifications.",
                    Details = ex.Message
                });
            }
        }

        [HttpGet("GetNumOfNotifi/{userId}")]
        public async Task<IActionResult> GetNumOfNotifi(string userId)
        {
            var noti = _context.Notifications.Count(n => n.User_Id == userId && n.isread == false);
            return Ok(noti);
        }
        [HttpPatch ("readNotifications/{userId}")]
        public async Task<IActionResult> ReadNotifications(string userId)
        {
            var noti = await _context.Notifications.Where(n => n.User_Id == userId && n.isread == false).ToListAsync();
            foreach (var n in noti)
            {
                n.isread= true;
            }
            await _context.SaveChangesAsync(); 

            return Ok("Notifications marked as read.");
        }
    }
}