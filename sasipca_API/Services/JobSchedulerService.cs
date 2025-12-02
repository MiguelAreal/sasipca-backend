using Hangfire;
using Microsoft.EntityFrameworkCore;
using sasipca_API.DBModels;
using sasipca_API.Enumerators;
using sasipca_API.Services.Interfaces;

namespace sasipca_API.Services
{
    public class JobSchedulerService : IJobSchedulerService
    {
        private readonly SasipcaContext _dbContext;
        private readonly ILogger<JobSchedulerService> _logger;
        private readonly INotificationService _notifService; // INJETADO AGORA

        public JobSchedulerService(
            SasipcaContext dbContext,
            ILogger<JobSchedulerService> logger,
            INotificationService notifService) // RECEBIDO NO CONSTRUTOR
        {
            _dbContext = dbContext;
            _logger = logger;
            _notifService = notifService;
        }

        // --------------------------------------------------------
        // MÉTODO 1: TRIGGER (Chamado na criação da entrega)
        // --------------------------------------------------------
        public void ScheduleDeliveryCheck(int deliveryId, DateOnly scheduledDate)
        {
            // ... (Lógica de cálculo de tempo mantém-se igual) ...
            var checkTime = scheduledDate.ToDateTime(new TimeOnly(23, 59, 0));

            if (checkTime < DateTime.Now)
            {
                checkTime = DateTime.Now.AddMinutes(10);
            }

            BackgroundJob.Schedule<IJobSchedulerService>(
                service => service.VerifyDeliveryStatus(deliveryId, scheduledDate),
                new DateTimeOffset(checkTime)
            );

            _logger.LogInformation($"Tarefa agendada para verificar Entrega #{deliveryId} em {checkTime}.");
        }

        // --------------------------------------------------------
        // MÉTODO 2: TAREFA (Executada pelo Hangfire no futuro)
        // --------------------------------------------------------
        [AutomaticRetry(Attempts = 3)]
        public async Task VerifyDeliveryStatus(int deliveryId, DateOnly expectedDate)
        {
            _logger.LogInformation($"[Job Hangfire] A verificar Entrega #{deliveryId}...");

            var delivery = await _dbContext.Deliveries
                .Include(d => d.Beneficiary)
                .Include(d => d.User) // Incluir o User (criador) para sabermos a quem notificar
                .FirstOrDefaultAsync(d => d.Id == deliveryId);

            if (delivery == null) return;

            if (delivery.ScheduledDate != expectedDate)
            {
                _logger.LogInformation($"[Job Abortado] A data da entrega mudou. Ignorando.");
                return;
            }

            if (delivery.StatusId == (int)Enums.DeliveryStatus.Agendada)
            {
                // Mudar estado para Cancelada
                delivery.StatusId = (int)Enums.DeliveryStatus.Cancelada;
                delivery.Note = (delivery.Note ?? "") + " [Sistema: Expirou automaticamente]";

                _logger.LogWarning($"Entrega #{deliveryId} expirou. Estado alterado para Cancelada.");

                // --- ENVIO DE NOTIFICAÇÃO ---
                // Notificar o criador da entrega
                await _notifService.SendNotificationAsync(
                    userId: delivery.UserId,
                    title: "Entrega Expirada",
                    message: $"A entrega para {delivery.Beneficiary.Name} agendada para {expectedDate} expirou e foi cancelada."
                );

                await _dbContext.SaveChangesAsync();
            }
            else
            {
                _logger.LogInformation($"Entrega #{deliveryId} já foi tratada. Nada a fazer.");
            }
        }


        // --------------------------------------------------------
        // GESTÃO DE VALIDADE (EXPIRY)
        // --------------------------------------------------------

        public void ScheduleExpiryCheck(int groupId, string productName, DateOnly expiryDate, int daysBefore)
        {
            if (daysBefore <= 0) return;

            var notificationDate = expiryDate.AddDays(-daysBefore).ToDateTime(new TimeOnly(8, 30, 0));

            if (notificationDate < DateTime.Now)
            {
                notificationDate = DateTime.Now.AddMinutes(10);
            }

            BackgroundJob.Schedule<IJobSchedulerService>(
                service => service.VerifyProductExpiry(groupId, daysBefore),
                new DateTimeOffset(notificationDate)
            );

            _logger.LogInformation($"Agendado aviso de validade para grupo #{groupId} ({productName}) em {notificationDate}.");
        }

        [AutomaticRetry(Attempts = 3)]
        public async Task VerifyProductExpiry(int groupId, int expectedDaysBefore)
        {
            var group = await _dbContext.ProductGroups
                .Include(g => g.BarcodeNavigation)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null) return;

            // Validações de Runtime
            if (group.Quantity <= 0)
            {
                _logger.LogInformation($"[Job Expiry Ignorado] Grupo #{groupId} já não tem stock.");
                return;
            }

            if (group.BarcodeNavigation.ExpNotif != expectedDaysBefore)
            {
                _logger.LogInformation($"[Job Expiry Ignorado] Configuração de dias mudou.");
                return;
            }

            // --- ENVIO DE NOTIFICAÇÃO ---
            var daysRemaining = group.ExpiryDate.DayNumber - DateOnly.FromDateTime(DateTime.Now).DayNumber;

            _logger.LogWarning($"[ALERTA VALIDADE] O produto {group.BarcodeNavigation.Name} expira em {daysRemaining} dias!");

            // Enviar para todos os utilizadores (Broadcast)
            // Isto garante que todos os voluntários/staff recebem o alerta de validade
            await _notifService.BroadcastNotification(
                title: "Aviso de Validade",
                message: $"O produto '{group.BarcodeNavigation.Name}' tem {group.Quantity} unidades que expiram a {group.ExpiryDate} ({daysRemaining} dias)."
            );
        }
    }
}