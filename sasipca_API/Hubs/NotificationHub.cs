using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace sasipca_API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        // O SignalR usa o UserIdentifier do JWT automaticamente para identificar o user.
    }
}