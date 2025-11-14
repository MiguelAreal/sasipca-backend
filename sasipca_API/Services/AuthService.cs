using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sasipca_API.DBModels;
using sasipca_API.Services.Interfaces;
using System.Security.Claims;

namespace sasipca_API.Services
{
    /// <summary>
    /// Serviço responsável pela autenticação e gestão de palavras-passe dos utilizadores.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly SasipcaContext _dbcontext;
        private readonly IJWTService _jwtService;
        private readonly int _refreshTokenValidityMinutes;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Construtor que inicializa o serviço com o IHttpContextAccessor e o contexto da base de dados.
        /// </summary>
        /// <param name="httpContextAccessor">Provedor de contexto HTTP para acessar informações sobre a requisição.</param>
        /// <param name="dbcontext">Contexto da base de dados para interagir com os dados de utilizadores e outros.</param>
        public AuthService(SasipcaContext dbcontext, IJWTService jwtService, IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _dbcontext = dbcontext;
            _jwtService = jwtService;
            _refreshTokenValidityMinutes = int.Parse(config["Jwt:RefreshTokenValidityInMinutes"] ?? "7200");
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Método para gerar um hash seguro de uma palavra-passe utilizando o algoritmo BCrypt.
        /// </summary>
        /// <param name="password">A palavra-passe a ser criptografada.</param>
        /// <returns>Uma string contendo o hash gerado da palavra-passe.</returns>
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
            
        /// <summary>
        /// Método para verificar se a palavra-passe fornecida corresponde ao hash armazenado.
        /// </summary>
        /// <param name="password">A palavra-passe fornecida para verificação.</param>
        /// <param name="hashedPassword">O hash da palavra-passe armazenado que será comparado.</param>
        /// <returns>Retorna um valor booleano indicando se as palavra-passes são iguais.</returns>
        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        /// <summary>
        /// Método auxiliar para obter o nome de um utilizador a partir do seu ID.
        /// </summary>
        /// <param name="userId">ID do utilizador.</param>
        /// <returns>O nome do utilizador ou null caso não exista.</returns>
        public async Task<string?> ObterNome(int userId)
        {
            return await _dbcontext.Users
                .Where(p => p.Id == userId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync();
        }


        /// <summary>
        /// Obtém o ID do utilizador logado a partir do header Authorization.
        /// </summary>
        public int? GetUserId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            var token = authHeader.Substring("Bearer ".Length).Trim();
            return _jwtService.ValidateAccessToken(token)?.ObterUserId();
        }

        public async Task<string> GerarOuManterRefreshToken(User user)
        {
            if (!string.IsNullOrEmpty(user.RefreshToken) && user.RefreshTokenExp > DateTime.Now)
                return user.RefreshToken;

            try
            {
                var refreshToken = _jwtService.GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExp = DateTime.Now.AddMinutes(_refreshTokenValidityMinutes);
                await _dbcontext.SaveChangesAsync();
                return refreshToken;
            }
            catch
            {
                user.RefreshToken = null;
                user.RefreshTokenExp = null;
                throw;
            }
        }

        public async Task<string> AtualizarRefreshTokenSeProximoExpirar(User user)
        {
            if ((user.RefreshTokenExp!.Value - DateTime.Now).TotalMinutes < 15)
            {
                user.RefreshToken = _jwtService.GenerateRefreshToken();
                user.RefreshTokenExp = DateTime.Now.AddMinutes(_refreshTokenValidityMinutes);
                await _dbcontext.SaveChangesAsync();
            }

            // Configura cookie
            _httpContextAccessor.HttpContext!.Response.Cookies.Append("refreshToken", user.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = user.RefreshTokenExp,
                IsEssential = true
            });

            return user.RefreshToken;
        }
    }
}
