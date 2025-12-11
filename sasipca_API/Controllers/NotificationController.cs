using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sasipca_API.Dtos;
using sasipca_API.Models;
using sasipca_API.Services.Interfaces;

namespace sasipca_API.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _service;
        private readonly ILogger<NotificationsController> _logger;
        private readonly IAuthService _authService;

        public NotificationsController(INotificationService service, IAuthService authService, ILogger<NotificationsController> logger)
        {
            _service = service;
            _authService = authService;
            _logger = logger;
        }

        // ----------------------------------------------------
        // OBTER LISTA DE NOTIFICAÇÕES (INBOX)
        // ----------------------------------------------------
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<NotificationGetDTO>))]
        public async Task<ActionResult<List<NotificationGetDTO>>> GetNotifications()
        {
            int? userId = _authService.GetUserId();
            if (userId == null) return Unauthorized(new Resposta("Utilizador não autenticado."));

            var notifications = await _service.GetUserNotificationsAsync(userId.Value);
            return Ok(notifications);
        }

        // ----------------------------------------------------
        // OBTER CONTAGEM DE NÃO LIDAS (BADGE)
        // ----------------------------------------------------
        [HttpGet("unread-count")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            int? userId = _authService.GetUserId();
            if (userId == null) return Unauthorized(new Resposta("Utilizador não autenticado."));

            var count = await _service.GetUnreadCountAsync(userId.Value);
            return Ok(count);
        }

        // ----------------------------------------------------
        // MARCAR COMO LIDA
        // ----------------------------------------------------
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            int? userId = _authService.GetUserId();
            if (userId == null) return Unauthorized(new Resposta("Utilizador não autenticado."));

            var success = await _service.MarkAsReadAsync(id, userId.Value);

            if (!success)
                return NotFound(new { message = "Notificação não encontrada." });

            return Ok();
        }

        // ----------------------------------------------------
        // APAGAR (ARQUIVAR)
        // ----------------------------------------------------
        [HttpDelete("{id}")]
        public async Task<IActionResult> ArchiveNotification(int id)
        {
            int? userId = _authService.GetUserId();
            if (userId == null) return Unauthorized(new Resposta("Utilizador não autenticado."));

            var success = await _service.ArchiveNotificationAsync(id, userId.Value);

            if (!success)
                return NotFound(new { message = "Notificação não encontrada." });

            return Ok(new { message = "Notificação arquivada." });
        }

        // ----------------------------------------------------
        // REGISTAR DISPOSITIVO (ANDROID/FCM)
        // ----------------------------------------------------
        [HttpPost("device")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterDevice([FromBody] DeviceTokenDTO dto)
        {
            // 1. Validar autenticação
            int? userId = _authService.GetUserId();
            if (userId == null) return Unauthorized(new Resposta("Utilizador não autenticado."));

            // 2. Validar body
            if (dto == null || string.IsNullOrWhiteSpace(dto.Token))
            {
                _logger.LogWarning($"User {userId} tentou registar dispositivo sem token.");
                return BadRequest(new { message = "O token é obrigatório." });
            }

            try
            {
                _logger.LogInformation($"A registar token FCM para o User {userId}...");

                // 3. Chamar o serviço
                await _service.RegisterDeviceAsync(userId.Value, dto.Token);

                _logger.LogInformation($"Sucesso: Token registado para User {userId}.");
                return Ok(new { message = "Dispositivo registado com sucesso." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao registar dispositivo para User {userId}: {ex.Message}");
                return StatusCode(500, new { message = "Erro interno ao registar dispositivo." });
            }
        }

        // ----------------------------------------------------
        // ENVIAR TESTE (PARA DEBUG)
        // ----------------------------------------------------
        [HttpPost("send-test")]
        public async Task<IActionResult> SendTestNotification([FromBody] TestNotificationDTO dto)
        {
            try
            {
                _logger.LogInformation($"A enviar notificação de teste para User {dto.TargetUserId}...");

                await _service.SendNotificationAsync(dto.TargetUserId, dto.Title, dto.Message);

                return Ok(new { message = "Notificação enviada para a fila de processamento." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao enviar teste: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }
    }

}