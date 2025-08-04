using Microsoft.AspNetCore.SignalR;

using Vstore.Hubs;
namespace Vstore.Services
{
    public class NotificationService
    {
        private readonly AppDBContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(AppDBContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task SendNotification(string userId, string title, string body)
        {
            // Save notification to the database
            var notification = new Notification
            {
                User_Id = userId,
                Title = title,
                Body = body,
                Notification_Message = $"{title}: {body}"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Notify the user via SignalR
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", title, body);
        }
        public async Task SendNotificationWithImage(string userId, string title, string body)
        {
            // Save notification to the database
            var notification = new Notification
            {
                User_Id = userId,
                Title = title,
                Body = body,
                DateTime = DateTime.Now,
                isread = false,
                Notification_Message = $"{title}: {body}"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Notify the user via SignalR with image
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotificationWithImage", title, body);
        }
        public async Task SendNotification2(string userId, string title, string body)
        {
            // Notify the user via SignalR
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", title, body);
        }
        public async Task SendNotificationsToUser(string userId)
        {
            // Fetch notifications for the user
            var notifications = await _context.Notifications
                .Where(n => n.User_Id == userId)
                .OrderByDescending(n => n.Notification_Id)
                .ToListAsync();

            // Send the notifications to the user via SignalR
            await _hubContext.Clients.Group(userId).SendAsync("ReceiveNotifications", notifications);
        }
        public async Task SendNotificationWithImage(string userId, string title, string body, string imageBase64)
        {
            // Notify the user via SignalR with the image
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotificationWithImage", title, body, imageBase64);
        }

    }
}

