using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using sasipca_API.DBModels;
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
                Message = $"{title}: {message}",
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
                Message = $"{title}: {message}",
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
    }
}