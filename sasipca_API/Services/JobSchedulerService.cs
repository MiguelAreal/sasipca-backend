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
        // private readonly INotificationService _notifService; // Injetar se fores enviar notificação

        public JobSchedulerService(SasipcaContext dbContext, ILogger<JobSchedulerService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // --------------------------------------------------------
        // MÉTODO 1: TRIGGER (Chamado na criação da entrega)
        // --------------------------------------------------------
        public void ScheduleDeliveryCheck(int deliveryId, DateOnly scheduledDate)
        {
            // Lógica: Se a entrega é para dia 20/10, queremos verificar se falhou
            // no final desse dia (ex: 23:59) ou no início do dia seguinte (ex: 09:00).

            // Vamos agendar para as 23:59 do próprio dia.
            var checkTime = scheduledDate.ToDateTime(new TimeOnly(23, 59, 0));

            // Se por acaso estamos a criar uma entrega para "hoje" às 23:55, 
            // damos uma margem de segurança de 10 minutos.
            if (checkTime < DateTime.Now)
            {
                checkTime = DateTime.Now.AddMinutes(10);
            }

            // AGENDAR NO HANGFIRE
            // Passamos a 'scheduledDate' como parâmetro para validação futura
            BackgroundJob.Schedule<IJobSchedulerService>(
                service => service.VerifyDeliveryStatus(deliveryId, scheduledDate),
                new DateTimeOffset(checkTime)
            );

            _logger.LogInformation($"Tarefa agendada para verificar Entrega #{deliveryId} em {checkTime}.");
        }

        // --------------------------------------------------------
        // MÉTODO 2: TAREFA (Executada pelo Hangfire no futuro)
        // --------------------------------------------------------
        [AutomaticRetry(Attempts = 3)] // Tenta 3 vezes se falhar por erro de BD
        public async Task VerifyDeliveryStatus(int deliveryId, DateOnly expectedDate)
        {
            _logger.LogInformation($"[Job Hangfire] A verificar Entrega #{deliveryId}...");

            var delivery = await _dbContext.Deliveries
                .Include(d => d.Beneficiary)
                .FirstOrDefaultAsync(d => d.Id == deliveryId);

            // 1. A entrega ainda existe?
            if (delivery == null) return;

            // 2. 
            // Verificamos se a data da entrega na BD ainda é a data que estávamos à espera.
            // Se for diferente, significa que o utilizador editou a entrega e este Job é "lixo" antigo.
            if (delivery.ScheduledDate != expectedDate)
            {
                _logger.LogInformation($"[Job Abortado] A data da entrega mudou (Era {expectedDate}, agora é {delivery.ScheduledDate}). Ignorando.");
                return;
            }

            // 3. Verificar Estado
            // Se ainda está "Agendada", então o prazo expirou sem confirmação.
            if (delivery.StatusId == (int)Enums.DeliveryStatus.Agendada)
            {
                // Mudar estado para Cancelada (ou Não Entregue)
                delivery.StatusId = (int)Enums.DeliveryStatus.Cancelada;

                // Opcional: Adicionar nota automática
                delivery.Note = (delivery.Note ?? "") + " [Sistema: Expirou automaticamente]";

                _logger.LogWarning($"Entrega #{deliveryId} expirou. Estado alterado para Cancelada.");

                // TODO: Enviar Notificação ao Criador
                // await _notifService.SendNotification(delivery.UserId, "Entrega Expirada", $"A entrega para {delivery.Beneficiary.Name} não foi confirmada.");

                await _dbContext.SaveChangesAsync();
            }
            else
            {
                _logger.LogInformation($"Entrega #{deliveryId} já foi tratada (Status: {delivery.StatusId}). Nada a fazer.");
            }
        }


        // --------------------------------------------------------
        // GESTÃO DE VALIDADE (EXPIRY)
        // --------------------------------------------------------

        public void ScheduleExpiryCheck(int groupId, string productName, DateOnly expiryDate, int daysBefore)
        {
            if (daysBefore <= 0) return;

            // Calcular a data do aviso: Validade - Dias de Aviso
            var notificationDate = expiryDate.AddDays(-daysBefore).ToDateTime(new TimeOnly(8, 30, 0)); // Avisar às 08:30 da manhã

            // Se a data de aviso já passou (ex: definimos aviso de 5 dias para algo que vence amanhã),
            // agendamos para "daqui a 10 minutos" para o utilizador ser avisado asap.
            if (notificationDate < DateTime.Now)
            {
                notificationDate = DateTime.Now.AddMinutes(10);
            }

            // Agendar no Hangfire
            BackgroundJob.Schedule<IJobSchedulerService>(
                service => service.VerifyProductExpiry(groupId, daysBefore),
                new DateTimeOffset(notificationDate)
            );

            _logger.LogInformation($"Agendado aviso de validade para Lote #{groupId} ({productName}) em {notificationDate} (Validade: {expiryDate}).");
        }



        [AutomaticRetry(Attempts = 3)]
        public async Task VerifyProductExpiry(int groupId, int expectedDaysBefore)
        {
            // 1. Buscar o Grupo
            var group = await _dbContext.ProductGroups
                .Include(g => g.BarcodeNavigation) // Incluir dados do Produto Pai
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null) return;

            // 2. VALIDAÇÃO INTELIGENTE (Runtime Validation)

            // A. O produto ainda tem stock? Se já foi tudo vendido/consumido, não chatear.
            if (group.Quantity <= 0)
            {
                _logger.LogInformation($"[Job Expiry Ignorado] Grupo #{groupId} já não tem stock.");
                return;
            }

            // B. A configuração de dias mudou?
            // Se o produto agora diz "Avisar 10 dias antes" e este Job era para "5 dias antes",
            // significa que este Job é antigo e deve ser ignorado (o novo Job tratará do aviso).
            if (group.BarcodeNavigation.ExpNotif != expectedDaysBefore)
            {
                _logger.LogInformation($"[Job Expiry Ignorado] Configuração de dias mudou (Era {expectedDaysBefore}, agora é {group.BarcodeNavigation.ExpNotif}).");
                return;
            }

            // 3. Enviar Notificação
            var daysRemaining = group.ExpiryDate.DayNumber - DateOnly.FromDateTime(DateTime.Now).DayNumber;

            _logger.LogWarning($"[ALERTA VALIDADE] O produto {group.BarcodeNavigation.Name} (Lote {groupId}) expira em {daysRemaining} dias!");

            // Enviar para todos os utilizadores ou admins
            // await _notifService.BroadcastNotification(
            //    title: "Aviso de Validade", 
            //    message: $"O produto '{group.BarcodeNavigation.Name}' tem {group.Quantity} unidades que expiram a {group.ExpiryDate}."
            // );
        }
    }
}