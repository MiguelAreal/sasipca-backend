using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sasipca_API.Services.Interfaces;
using System.Security.Claims;
using sasipca_API.Models; // Para usar a classe Resposta se a tiveres, senão usa objectos anónimos

namespace sasipca_API.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    [Authorize] // Garante que apenas users logados acedem
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _service;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(INotificationService service, ILogger<NotificationsController> logger)
        {
            _service = service;
            _logger = logger;
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
            // 1. Validação básica do body
            if (dto == null || string.IsNullOrWhiteSpace(dto.Token))
            {
                _logger.LogWarning("Tentativa de registo de dispositivo sem token.");
                return BadRequest(new { message = "O token é obrigatório." });
            }

            // 2. Extrair ID do utilizador do Token JWT
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                _logger.LogError("Token JWT inválido ou sem NameIdentifier.");
                return Unauthorized();
            }

            try
            {
                _logger.LogInformation($"A registar token FCM para o User {userId}...");

                // 3. Chamar o serviço
                await _service.RegisterDeviceAsync(userId, dto.Token);

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

    // ----------------------------------------------------
    // DTOs (Data Transfer Objects)
    // ----------------------------------------------------

    // Este DTO tem de bater certo com o JSON enviado pelo Kotlin: {"token": "..."}
    public class DeviceTokenDTO
    {
        public string Token { get; set; }
    }

    public class TestNotificationDTO
    {
        public int TargetUserId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
    }
}