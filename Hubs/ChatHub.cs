using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System; // Make sure to include this for DateTime

namespace EMS.Api.Hubs
{
    public class ChatHub : Hub
    {
        // This method will be called from our ChatController to broadcast messages
        public async Task SendMessageToAll(int messageId, int userId, string userName, string message, DateTime createdAt)
        {
            // This sends a "ReceiveMessage" event to all connected clients
            await Clients.All.SendAsync("ReceiveMessage", messageId, userId, userName, message, createdAt);
        }
    }
}