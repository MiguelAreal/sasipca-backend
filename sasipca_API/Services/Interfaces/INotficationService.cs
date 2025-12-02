using sasipca_API.Dtos;

namespace sasipca_API.Services.Interfaces
{
    public interface INotificationService
    {
        Task RegisterDeviceAsync(int userId, string token);
        Task SendNotificationAsync(int userId, string title, string message);
        Task BroadcastNotification(string title, string message);

    }
}
