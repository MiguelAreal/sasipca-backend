using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Models;
using sasipca_API.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace sasipca_API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly SasipcaContext _dbcontext;
        private readonly IAuthService _authService;
        private readonly IJWTService _jwtService;
        private readonly int _refreshTokenValidityMinutes;

        public AuthController(IAuthService authService, IJWTService jwtService, SasipcaContext context, IConfiguration config)
        {
            _dbcontext = context;
            _authService = authService;
            _jwtService = jwtService;
            _refreshTokenValidityMinutes = int.Parse(config["Jwt:RefreshTokenValidityInMinutes"] ?? "7200");
        }

        [HttpPost("login/microsoft")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginMicrosoft([FromBody] MicrosoftLoginDTO loginDto)
        {
            var azureClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");

            if (string.IsNullOrEmpty(loginDto.IdToken))
                return BadRequest(new Resposta("id_token não fornecido."));

            try
            {
                // Validação do token Microsoft
                var handler = new JwtSecurityTokenHandler();
                var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration",
                    new OpenIdConnectConfigurationRetriever());

                var openIdConfig = await configManager.GetConfigurationAsync();

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = openIdConfig.SigningKeys,
                    ValidateIssuer = false,
                    ValidateAudience = true,
                    ValidAudience = azureClientId,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = handler.ValidateToken(loginDto.IdToken, validationParameters, out var validatedToken);

                // Extrair email
                var email = principal.FindFirst("preferred_username")?.Value
                            ?? principal.FindFirst("upn")?.Value
                            ?? principal.FindFirst("email")?.Value;

                if (string.IsNullOrEmpty(email) || !email.EndsWith("ipca.pt"))
                    return Unauthorized(new Resposta("Utilizador não autorizado."));

                // Extrair número mecanográfico do e-mail
                // Ex.: a12345@alunos.ipca.pt -> 12345
                var mecanograficoStr = email.Split('@')[0].TrimStart('a', 'A');
                if (!int.TryParse(mecanograficoStr, out int mecanografico))
                    return Unauthorized(new Resposta("Número mecanográfico inválido."));

                // Procurar user pelo número mecanográfico (id)
                var user = await _dbcontext.Users.FindAsync(mecanografico);
                if (user == null)
                    return Unauthorized(new Resposta("Utilizador não registado."));

                // Atualizar nome/email se necessário
                var nomeDoToken = principal.FindFirst("name")?.Value
                                 ?? (principal.FindFirst("given_name")?.Value + " " + principal.FindFirst("family_name")?.Value);

                if (!string.IsNullOrEmpty(nomeDoToken) && user.Name != nomeDoToken)
                {
                    user.Name = nomeDoToken;
                    user.Email = email; // mantém sincronizado
                    await _dbcontext.SaveChangesAsync();
                }

                // Gerar JWT interno + refresh token
                var accessToken = _jwtService.GenerateToken(user.Id, user.Email);
                var refreshToken = await _authService.GerarOuManterRefreshToken(user);

                Response.Cookies.Append("refreshToken", refreshToken, new Microsoft.AspNetCore.Http.CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None,
                    Expires = user.RefreshTokenExp,
                    IsEssential = true
                });

                return Ok(new AuthResponse(accessToken, refreshToken, user.Id, user.Name));
            }
            catch (SecurityTokenValidationException stvEx)
            {
                return Unauthorized(new Resposta($"Token inválido: {stvEx.Message}"));
            }
            catch (Exception ex)
            {
                return BadRequest(new Resposta($"Erro no login Microsoft: {ex.Message}"));
            }
        }


        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken()
        {
            if (!Request.Cookies.TryGetValue("refreshToken", out string refreshToken))
                return BadRequest(new Resposta("Sessão expirada. Efetue login novamente."));

            var expiredToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (string.IsNullOrEmpty(expiredToken))
                return BadRequest(new Resposta("Token de acesso não fornecido."));

            var principal = _jwtService.GetPrincipalFromExpiredToken(expiredToken);
            if (principal == null)
                return Unauthorized(new Resposta("Token de acesso inválido."));

            if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return Unauthorized(new Resposta("Identificação do utilizador inválida."));

            var user = await _dbcontext.Users.FindAsync(userId);
            if (user == null || user.RefreshToken != refreshToken || !user.RefreshTokenExp.HasValue)
                return Unauthorized(new Resposta("Sessão inválida. Efetue login novamente."));

            if (user.RefreshTokenExp <= DateTime.Now)
                return Unauthorized(new Resposta("Sessão expirada. Efetue login novamente."));

            var novoAccessToken = _jwtService.GenerateToken(user.Id, user.Email);
            var novoRefreshToken = await _authService.AtualizarRefreshTokenSeProximoExpirar(user);

            return Ok(new AuthResponse(novoAccessToken, novoRefreshToken, user.Id, user.Name));
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = _authService.GetUserId();
            if (userId == null)
                return Unauthorized(new Resposta("Utilizador não autenticado."));

            var user = await _dbcontext.Users.FindAsync(userId);
            if (user == null)
                return Unauthorized(new Resposta("Utilizador não encontrado."));

            user.RefreshToken = null;
            user.RefreshTokenExp = null;
            await _dbcontext.SaveChangesAsync();

            Response.Cookies.Delete("refreshToken", new Microsoft.AspNetCore.Http.CookieOptions
            {
                Secure = true,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None
            });

            return Ok(new Resposta("Sessão terminada com sucesso."));
        }
    }
}
