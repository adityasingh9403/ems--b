using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace EMS.Api.Hubs;

// Yeh ek simple hub hai. Iska kaam sirf clients ko updates bhejna hai.
public class NotificationHub : Hub
{
    // Hum frontend se is hub par koi method call nahi karenge,
    // isliye yeh khaali rahega. Iska istemal sirf server-to-client
    // communication ke liye hoga.
}
