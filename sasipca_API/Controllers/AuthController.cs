using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using sasipca_API.Attributes;
using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Enumerators; // Importante para o Enum
using sasipca_API.Models;
using sasipca_API.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using static sasipca_API.Enumerators.Enums;

namespace sasipca_API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly SasipcaContext _dbcontext;
        private readonly IJWTService _jwtService;
        private readonly int _refreshTokenValidityMinutes;

        public AuthController(IJWTService jwtService, SasipcaContext context, IConfiguration config)
        {
            _dbcontext = context;
            _jwtService = jwtService;
            _refreshTokenValidityMinutes = int.Parse(config["Jwt:RefreshTokenValidityInMinutes"] ?? "10080"); // 7 dias default
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginMicrosoft([FromBody] MicrosoftLoginDTO loginDto)
        {
            var azureClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");

            if (string.IsNullOrEmpty(loginDto.IdToken))
                return BadRequest(new Resposta("IdToken não fornecido."));

            try
            {
                // 1. Configuração e Validação do Token
                var handler = new JwtSecurityTokenHandler();

                // Nota: O JwtSecurityTokenHandler por defeito tenta mapear claims (ex: "name" -> ClaimTypes.Name).
                // Para garantir que lemos as claims RAW, podemos desativar o mapeamento

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

                // =================================================================================
                // 2. EXTRAÇÃO DE DADOS OTIMIZADA (Baseada no Token)
                // =================================================================================

                // EMAIL
                var email = principal.FindFirst("preferred_username")?.Value
                            ?? principal.FindFirst("email")?.Value;

                if (string.IsNullOrEmpty(email) || !email.EndsWith("ipca.pt"))
                    return Unauthorized(new Resposta("Domínio de e-mail inválido."));

                // NOME
                var rawName = principal.FindFirst("name")?.Value ?? principal.FindFirst(ClaimTypes.Name)?.Value;
                string nomeFormatado = "Utilizador";

                if (!string.IsNullOrEmpty(rawName))
                {
                    var parts = rawName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        // Pega o Primeiro e o Último (Ex: "Miguel" + "Areal")
                        nomeFormatado = $"{parts[0]} {parts[^1]}";
                    }
                    else
                    {
                        nomeFormatado = rawName;
                    }
                }

                // =================================================================================
                // 3. LÓGICA DE PRIORIDADE (Admin > Beneficiário)
                // =================================================================================

                UserRole role;
                int internalId;
                string userName;
                string refreshToken = GenerateRefreshTokenString();
                DateTime refreshTokenExp = DateTime.Now.AddMinutes(_refreshTokenValidityMinutes);

                // A. Verificar ADMIN
                var adminUser = await _dbcontext.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (adminUser != null)
                {
                    role = UserRole.Admin;
                    internalId = adminUser.Id;
                    userName = adminUser.Name ?? nomeFormatado; // Usa o da BD se existir, senão usa o do token

                    // Se for o primeiro login (nome null na BD) ou quisermos atualizar sempre:
                    if (adminUser.Name != nomeFormatado)
                        adminUser.Name = nomeFormatado;

                    adminUser.RefreshToken = refreshToken;
                    adminUser.RefreshTokenExp = refreshTokenExp;
                }
                else
                {
                    // B. Verificar BENEFICIÁRIO
                    var beneficiary = await _dbcontext.Beneficiaries.FirstOrDefaultAsync(b => b.Email == email);

                    if (beneficiary != null)
                    {
                        role = UserRole.Beneficiary;
                        internalId = beneficiary.Id;
                        userName = beneficiary.Name; // Beneficiários usam o nome registado na BD

                        beneficiary.RefreshToken = refreshToken;
                        beneficiary.RefreshTokenExp = refreshTokenExp;
                    }
                    else
                    {
                        return Unauthorized(new Resposta("Utilizador não registado no sistema."));
                    }
                }

                // 4. Gravar na BD
                await _dbcontext.SaveChangesAsync();

                // 5. Gerar Tokens e Retornar
                var accessToken = _jwtService.GenerateToken(internalId, email, role.ToString());
                SetRefreshTokenCookie(refreshToken, refreshTokenExp);

                return Ok(new AuthResponse(accessToken, refreshToken, internalId, userName, role.ToString()));
            }
            catch (Exception ex)
            {
                return BadRequest(new Resposta($"Erro na autenticação: {ex.Message}"));
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

            // Recuperar dados do Token Expirado
            if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return Unauthorized(new Resposta("Identificação do utilizador inválida."));

            var email = principal.FindFirstValue(ClaimTypes.Email);
            var roleStr = principal.FindFirstValue(ClaimTypes.Role);

            if (!Enum.TryParse(roleStr, out UserRole role))
                return Unauthorized(new Resposta("Role inválida."));

            // =================================================================================
            // LÓGICA DE REFRESH POR TIPO DE UTILIZADOR
            // =================================================================================

            string newAccessToken;
            string newRefreshToken = GenerateRefreshTokenString();
            string userName = "";

            if (role == UserRole.Admin)
            {
                var user = await _dbcontext.Users.FindAsync(userId);
                if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExp <= DateTime.Now)
                    return Unauthorized(new Resposta("Sessão inválida ou expirada."));

                userName = user.Name;
                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExp = DateTime.Now.AddMinutes(_refreshTokenValidityMinutes);
            }
            else if (role == UserRole.Beneficiary)
            {
                var beneficiary = await _dbcontext.Beneficiaries.FindAsync(userId);
                if (beneficiary == null || beneficiary.RefreshToken != refreshToken || beneficiary.RefreshTokenExp <= DateTime.Now)
                    return Unauthorized(new Resposta("Sessão inválida ou expirada."));

                userName = beneficiary.Name;
                beneficiary.RefreshToken = newRefreshToken;
                beneficiary.RefreshTokenExp = DateTime.Now.AddMinutes(_refreshTokenValidityMinutes);
            }
            else
            {
                return Unauthorized(new Resposta("Tipo de utilizador desconhecido."));
            }

            await _dbcontext.SaveChangesAsync();

            newAccessToken = _jwtService.GenerateToken(userId, email, role.ToString());

            // Atualizar cookie
            SetRefreshTokenCookie(newRefreshToken, DateTime.Now.AddMinutes(_refreshTokenValidityMinutes));

            return Ok(new AuthResponse(newAccessToken, newRefreshToken, userId, userName, role.ToString()));
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            // Ler dados do token atual
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var roleStr = User.FindFirstValue(ClaimTypes.Role);

            if (userIdStr == null || roleStr == null || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new Resposta("Utilizador não autenticado."));

            Enum.TryParse(roleStr, out UserRole role);

            // Limpar na tabela correta
            if (role == UserRole.Admin)
            {
                var user = await _dbcontext.Users.FindAsync(userId);
                if (user != null)
                {
                    user.RefreshToken = null;
                    user.RefreshTokenExp = null;
                }
            }
            else if (role == UserRole.Beneficiary)
            {
                var beneficiary = await _dbcontext.Beneficiaries.FindAsync(userId);
                if (beneficiary != null)
                {
                    beneficiary.RefreshToken = null;
                    beneficiary.RefreshTokenExp = null;
                }
            }

            await _dbcontext.SaveChangesAsync();

            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                Secure = true,
                SameSite = SameSiteMode.None
            });

            return Ok(new Resposta("Sessão terminada com sucesso."));
        }


        

        // --- Helpers ---

        private static string GenerateRefreshTokenString()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private void SetRefreshTokenCookie(string refreshToken, DateTime expires)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = expires,
                IsEssential = true
            };
            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
    }
}