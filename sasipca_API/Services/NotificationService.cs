using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Enumerators;
using sasipca_API.Hubs;
using sasipca_API.Services.Interfaces;

namespace sasipca_API.Services
{
    public class NotificationService : INotificationService
    {
        private readonly SasipcaContext _dbContext;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(SasipcaContext dbContext, IHubContext<NotificationHub> hubContext, ILogger<NotificationService> logger)
        {
            _dbContext = dbContext;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task RegisterDeviceAsync(int userId, string token)
        {
            var device = await _dbContext.UserDevices.FirstOrDefaultAsync(d => d.FcmToken == token);
            if (device == null)
            {
                _dbContext.UserDevices.Add(new UserDevice { UserId = userId, FcmToken = token });
            }
            else
            {
                device.UserId = userId; // Atualiza o dono do dispositivo se mudar
            }
            await _dbContext.SaveChangesAsync();
        }

        public async Task SendNotificationAsync(int userId, string title, string message)
        {
            // 1. Guardar no Histórico (Base de Dados)
            var notif = new DBModels.Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                StatusId = (int)Enums.NotificationStatus.NaoLida
            };
            _dbContext.Notifications.Add(notif);
            await _dbContext.SaveChangesAsync();

            // 2. Enviar para Desktop (SignalR)
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", title, message);

            // 3. Enviar para Android (Firebase)
            var tokens = await _dbContext.UserDevices
                .Where(u => u.UserId == userId)
                .Select(t => t.FcmToken)
                .ToListAsync();

            if (tokens.Any())
            {
                await SendFcmBatchAsync(tokens, title, message);
            }
        }

        // --- IMPLEMENTAÇÃO DO BROADCAST ---
        public async Task BroadcastNotification(string title, string message)
        {
            // 1. Enviar para TODOS os Desktops ligados (SignalR)
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", title, message);

            // 2. Enviar para TODOS os Androids (Firebase)
            // Obtém todos os tokens existentes na BD
            var allTokens = await _dbContext.UserDevices
                .Select(d => d.FcmToken)
                .ToListAsync();

            if (allTokens.Any())
            {
                // O Firebase limita o Multicast a 500 tokens por envio.
                // Vamos dividir em lotes de 500 (Batching).
                const int BatchSize = 500;
                for (int i = 0; i < allTokens.Count; i += BatchSize)
                {
                    var batch = allTokens.Skip(i).Take(BatchSize).ToList();
                    await SendFcmBatchAsync(batch, title, message);
                }
            }

            // 3. Guardar no Histórico para TODOS os utilizadores
            // Nota: Se tiveres milhares de utilizadores, isto pode ser lento.
            // Para a escala do IPCA, é aceitável.
            var allUserIds = await _dbContext.Users.Select(u => u.Id).ToListAsync();

            var notifications = allUserIds.Select(uid => new DBModels.Notification
            {
                UserId = uid,
                Title = title,
                Message = message,
                StatusId = (int)Enums.NotificationStatus.NaoLida
            });

            await _dbContext.Notifications.AddRangeAsync(notifications);
            await _dbContext.SaveChangesAsync();
        }

        // --- HELPER PRIVADO PARA ENVIAR BATCHES FCM ---
        private async Task SendFcmBatchAsync(List<string> tokens, string title, string message)
        {
            var multicast = new MulticastMessage
            {
                Tokens = tokens,
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = title,
                    Body = message
                }
            };

            try
            {
                await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(multicast);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro Firebase Broadcast: {ex.Message}");
            }
        }

        public async Task<List<NotificationGetDTO>> GetUserNotificationsAsync(int userId)
        {
            // Busca notificações que NÃO estejam arquivadas
            // Ordena da mais recente para a mais antiga
            return await _dbContext.Notifications
                .Where(n => n.UserId == userId && n.StatusId != (int)Enums.NotificationStatus.Arquivada)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationGetDTO
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Date = n.CreatedAt,
                    IsRead = n.StatusId == (int)Enums.NotificationStatus.Lida
                })
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _dbContext.Notifications
                .CountAsync(n => n.UserId == userId && n.StatusId == (int)Enums.NotificationStatus.NaoLida);
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            var notif = await _dbContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notif == null) return false;

            // Só atualiza se ainda não estiver lida para poupar escrita
            if (notif.StatusId == (int)Enums.NotificationStatus.NaoLida)
            {
                notif.StatusId = (int)Enums.NotificationStatus.Lida;
                await _dbContext.SaveChangesAsync();
            }
            return true;
        }

        public async Task<bool> ArchiveNotificationAsync(int notificationId, int userId)
        {
            var notif = await _dbContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notif == null) return false;

            // Arquivar ("Apagar" logicamente)
            notif.StatusId = (int)Enums.NotificationStatus.Arquivada;
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}