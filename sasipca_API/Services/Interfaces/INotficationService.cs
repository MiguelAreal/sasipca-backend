using sasipca_API.Dtos;

namespace sasipca_API.Services.Interfaces
{
    public interface INotificationService
    {
        // Métodos de envio
        Task RegisterDeviceAsync(int userId, string token);
        Task SendNotificationAsync(int userId, string title, string message);
        Task BroadcastNotification(string title, string message);

        // --- MÉTODOS PARA O INBOX ---
        Task<List<NotificationGetDTO>> GetUserNotificationsAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
        Task<bool> ArchiveNotificationAsync(int notificationId, int userId);
    }
}