using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sasipca_API.DBModels;
using sasipca_API.Services.Interfaces;
using System.Security.Claims;

[Route("api/notifications")]
[ApiController]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service)
    {
        _service = service;
    }

    [HttpPost("register-device")]
    public async Task<IActionResult> RegisterDevice([FromBody] string token) // Recebe string JSON
    {
        // Se vier como objeto JSON { "token": "..." }, cria um DTO.
        // Aqui assumo body simples ou DTO. Vamos usar um DTO simples é melhor.
        return BadRequest("Use um DTO");
    }

    // Melhor usar este DTO inline
    public class DeviceTokenDTO { public string Token { get; set; } }

    [HttpPost("device")]
    public async Task<IActionResult> RegisterDeviceDto([FromBody] DeviceTokenDTO dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        await _service.RegisterDeviceAsync(userId, dto.Token);
        return Ok();
    }
}