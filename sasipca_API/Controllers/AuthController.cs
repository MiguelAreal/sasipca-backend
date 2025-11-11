using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NuGet.Common;
using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Enumerators;
using sasipca_API.Models;
using sasipca_API.Services;
using sasipca_API.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace sasipca_API.Controllers
{
    /// <summary>
    /// AuthController Tem como objetivo suportar todas os endpoints relacionados com autenticação de utilizador.
    /// </summary>
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly SasipcaContext _dbcontext;
        private readonly IAuthService _authService;
        private readonly IJWTService _jwtService;
        private readonly IEmailService _emailService;
        private readonly int _refreshTokenValidityMinutes;
        private readonly INotificationService _notificacaoService;

        /// <summary>
        /// Inicialização do AuthController.
        /// </summary>
        /// <remarks>
        /// Instancia todos os serviços necessários.
        /// </remarks>
        /// <param name="authService"></param>
        /// <param name="jwtService"></param>
        /// <param name="emailService"></param>
        /// <param name="context"></param>
        /// <param name="config"></param>
        /// <param name="notificacaoService"></param>
        public AuthController(IAuthService authService, IJWTService jwtService, IEmailService emailService, SasipcaContext context, IConfiguration config, INotificationService notificacaoService)
        {
            _dbcontext = context;
            _authService = authService;
            _jwtService = jwtService;
            _emailService = emailService;
            _refreshTokenValidityMinutes = int.Parse(config["Jwt:RefreshTokenValidityInMinutes"] ?? "7200");
            _notificacaoService = notificacaoService;   

        }

        /// <summary>
        /// Login de utilizador.
        /// Retorna access token, e o ID do user.
        /// </summary>
        /// <remarks>
        /// Gera um refresh token (armazenado em cookie HTTP-only) e um access token (retornado no corpo da resposta).
        /// O access token é válido por 10 minutos, enquanto o refresh token tem validade configurável (default: 7200 minutos/5 dias).
        /// 
        /// </remarks>
        /// <param name="userLoginDto">Objeto com credenciais de login</param>
        /// <returns>Access token JWT</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [Produces("application/json")]
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserLoginDTO userLoginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new Resposta("Credenciais inválidas. Verifique os dados introduzidos."));
            }

            try
            {
                var user = await _dbcontext.Users
                    .FirstOrDefaultAsync(u => u.Email == userLoginDto.Email);

                if (user == null || !_authService.VerifyPassword(userLoginDto.Password, user.Password))
                {
                    return BadRequest(new Resposta("Credenciais inválidas. Verifique os dados introduzidos."));
                }

                // Gera AccessToken
                var accessToken = _jwtService.GenerateToken(user.Id, user.Email);

                //Gera ou busca RefreshToken
                var refreshToken = await GerarOuManterRefreshToken(user);

                // Configura cookie
                Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = user.RefreshTokenExp,
                    IsEssential = true
                });

                return Ok(new AuthResponse(accessToken,refreshToken, user.Id,user.Name));
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro durante o login. Tente novamente."));
            }
        }

        /// <summary>
        /// Renova o access token utilizando o refresh token.
        /// </summary>
        /// <remarks>
        /// Requer:
        /// 1. Cookie com refresh token válido
        /// 2. Header Authorization com o access token expirado
        /// 
        /// Gera um novo access token se o refresh token for válido.
        /// O refresh token é automaticamente renovado se estiver perto da expiração (menos de 15 minutos).
        /// </remarks>
        /// <returns>Novo access token JWT</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [Produces("application/json")]
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                // 1. Obter refresh token do cookie
                if (!Request.Cookies.TryGetValue("refreshToken", out string refreshToken))
                {
                    return BadRequest(new Resposta("Sessão expirada. Efetue login novamente."));
                }

                // 2. Obter Access Token expirado do header
                var expiredToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (string.IsNullOrEmpty(expiredToken))
                {
                    return BadRequest(new Resposta("Token de acesso não fornecido."));
                }

                // 3. Validar token expirado (ignorando expiração)
                var principal = _jwtService.GetPrincipalFromExpiredToken(expiredToken);
                if (principal == null)
                {
                    return Unauthorized(new Resposta("Token de acesso inválido."));
                }

                // 4. Obter ID do utilizador vindo do token diretamente
                if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                {
                    return Unauthorized(new Resposta("Identificação do utilizador inválida."));
                }


                // 5. Validar refresh token
                var user = await _dbcontext.Users.FindAsync(userId);
                if (user == null || user.RefreshToken != refreshToken || !user.RefreshTokenExp.HasValue)
                {
                    return Unauthorized(new Resposta("Sessão inválida. Efetue login novamente."));
                }

                if (user.RefreshTokenExp <= DateTime.Now)
                {
                    return Unauthorized(new Resposta("Sessão expirada. Efetue login novamente."));
                }

                // 6. Gerar novo accesstoken e ver se é preciso novo RefreshToken
                var novoAccessToken = _jwtService.GenerateToken(user.Id, user.Email);

                var novoRefreshToken = await AtualizarRefreshTokenSeProximoExpirar(user);

                return Ok(new AuthResponse(novoAccessToken,novoRefreshToken, user.Id,user.Name));
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Erro ao renovar a sessão. Tente novamente."));
            }
        }

        /// <summary>
        /// Termina a sessão do utilizador.
        /// </summary>
        /// <remarks>
        /// Invalida o refresh token atual, removendo-o da base de dados e das cookies.
        /// Requer autenticação com um access token válido.
        /// </remarks>
        /// <returns>Mensagem de confirmação</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [Produces("application/json")]
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                int userId = (int)HttpContext.Items["UserId"];

                var pessoa = await _dbcontext.Users.FindAsync(userId);

                if (pessoa == null)
                {
                    return Unauthorized(new Resposta("Utilizador não encontrado."));
                }

                // Invalida o refresh token
                pessoa.RefreshToken = null;
                pessoa.RefreshTokenExp = null;
                await _dbcontext.SaveChangesAsync();

                // Remove o cookie
                Response.Cookies.Delete("refreshToken", new CookieOptions
                {
                    Secure = true,
                    SameSite = SameSiteMode.None
                });

                return Ok(new Resposta("Sessão terminada com sucesso."));
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao terminar a sessão."));
            }
        }

        

        /// <summary>
        /// Solicitação de redefinição de password.
        /// </summary>
        /// <remarks>
        /// Envia um e-mail com um link para redefinir a password.
        /// O link contém um token válido por 1 hora.
        /// 
        /// Exemplo de requisição:
        /// POST /api//password/forgot
        /// {
        ///     "email": "utilizador@exemplo.pt"
        /// }
        /// </remarks>
        /// <param name="esqueciPwdDto">Objeto com e-mail do utilizador</param>
        /// <returns>Mensagem de confirmação</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [HttpPost("/password/forgot")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] EsqueciPwdDTO esqueciPwdDto)
        {
            try
            {
                var user = await _dbcontext.Users
                    .FirstOrDefaultAsync(p => p.Email == esqueciPwdDto.Email);

                if (user != null)
                {
                    // Não revelar que o e-mail não existe por questões de segurança
                    return Ok(new Resposta("Se o e-mail existir, será enviado um link de redefinição."));
                }

                // Remove tokens antigos
                await _dbcontext.TokenResetPasswords
                    .Where(t => t.User == user)
                    .ExecuteDeleteAsync();

                // Gera novo token
                var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                var tokenRedefinicao = new DBModels.TokenResetPassword
                {
                    UserId = user.Id,
                    Token = token,
                    ExpDate = DateTime.Now.AddHours(1)
                };

                await _dbcontext.TokenResetPasswords.AddAsync(tokenRedefinicao);
                await _dbcontext.SaveChangesAsync();

                // Envia e-mail
                var link = $"https://neighbourlink.pt/redefinir-password?token={Uri.EscapeDataString(token)}";
                var placeholders = new Dictionary<string, string> { { "link", link } };

                /*await _emailService.SendEmailAsync(
                    user.Email,
                    "Redefinição de Palavra-Passe",
                    "ForgotPasswordTemplate",
                    placeholders);*/

                return Ok(new Resposta("Link de redefinição enviado para o seu e-mail."));
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Erro ao processar pedido. Tente novamente."));
            }
        }

        /// <summary>
        /// Redefine a password do utilizador.
        /// </summary>
        /// <remarks>
        /// Requer um token válido obtido através do endpoint 'forgot-password'.
        /// 
        /// Exemplo de requisição:
        /// POST /api/password/reset
        /// {
        ///     "token": "token_gerado",
        ///     "novaPassword": "novaPasswordSegura123"
        /// }
        /// </remarks>
        /// <param name="atribuirNovaPwdDTO">Objeto com token e nova password</param>
        /// <returns>Mensagem de confirmação</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [HttpPost("/password/reset")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] AtribuirNovaPwdDTO atribuirNovaPwdDTO)
        {
            try
            {
                var tokenDecodificado = Uri.UnescapeDataString(atribuirNovaPwdDTO.Token);
                var tokenRedefinicao = await _dbcontext.TokenResetPasswords
                    .FirstOrDefaultAsync(t => t.Token == tokenDecodificado);

                if (tokenRedefinicao == null || tokenRedefinicao.ExpDate < DateTime.Now)
                {
                    return BadRequest(new Resposta("Link inválido ou expirado."));
                }

                var user = await _dbcontext.Users.FindAsync(tokenRedefinicao.UserId);
                if (user == null)
                {
                    return BadRequest(new Resposta("Utilizador não encontrado."));
                }

                // Atualiza password e remove tokens
                user.Password = _authService.HashPassword(atribuirNovaPwdDTO.NovaPassword);
                await _dbcontext.TokenResetPasswords
                    .Where(t => t.User == user)
                    .ExecuteDeleteAsync();

                await _dbcontext.SaveChangesAsync();

                return Ok(new Resposta("Password redefinida com sucesso."));
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Erro ao redefinir password. Tente novamente."));
            }
        }


        #region Métodos Auxiliares Privados

        private async Task<string> GerarOuManterRefreshToken(User user)
        {
            // Se já tem um refresh token válido, mantém
            if (!string.IsNullOrEmpty(user.RefreshToken) &&
                user.RefreshTokenExp > DateTime.Now)
            {
                return user.RefreshToken;
            }

            // Se não tem nenhum válido, gera um novo
            try
            {
                var refreshToken = _jwtService.GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExp = DateTime.Now.AddMinutes(_refreshTokenValidityMinutes);

                await _dbcontext.SaveChangesAsync();
                return refreshToken;
            }
            catch (Exception ex)
            {
                // Rollback em caso de erro
                user.RefreshToken = null;
                user.RefreshTokenExp = null;
                throw; // Será capturado no método Login
            }
           

        }

        private async Task<string> AtualizarRefreshTokenSeProximoExpirar(User user)
        {
            // Verifica se está próximo de expirar (menos de 15 minutos)
            if ((user.RefreshTokenExp!.Value - DateTime.Now).TotalMinutes < 15)
            {
                user.RefreshToken = _jwtService.GenerateRefreshToken();
                user.RefreshTokenExp = DateTime.Now.AddMinutes(_refreshTokenValidityMinutes);
                await _dbcontext.SaveChangesAsync();
            }

            // Configura cookie (mesmo que seja o mesmo token)
            Response.Cookies.Append("refreshToken", user.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = user.RefreshTokenExp,
                IsEssential = true
            });

            return user.RefreshToken;
        }

        #endregion
    }
}
