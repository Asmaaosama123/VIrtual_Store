using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace Vstore.Hubs
{
    public class NotificationHub:Hub
    {
        public async Task NotifyUser(string userId, string title, string message)
        {
            // Sends a notification to a specific user
            await Clients.User(userId).SendAsync("ReceiveNotification", title, message);
        }
        public override async Task OnConnectedAsync()
        {
            // Retrieve userId from query string
            var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();
            if (!string.IsNullOrEmpty(userId))
            {
                // Map the userId to the SignalR connection
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}

