using Microsoft.IdentityModel.Tokens;
using sasipca_API.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace sasipca_API.Services
{
    /// <summary>
    /// Serviço responsável por gerar e validar tokens JWT para autenticação de utilizadores.
    /// </summary>
    public class JWTService: IJWTService
    {
        private readonly JwtSettings _settings;
        private readonly JwtSecurityTokenHandler _tokenHandler = new();
        private readonly string _key;

        /// <summary>
        /// Inicializa o serviço com as configurações JWT.
        /// </summary>
        public JWTService(IConfiguration config, string key = null, JwtSecurityTokenHandler tokenHandler = null)
        {
            _key = key ?? (Environment.GetEnvironmentVariable("JWT_KEY") ?? throw new ArgumentNullException("JWT_KEY não está definida no ambiente."));
            _settings = new JwtSettings(
                _key,
                config["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer não definido no appsettings."),
                config["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience não definido no appsettings."),
                int.Parse(config["Jwt:TokenValidityInMinutes"] ?? throw new ArgumentNullException("Jwt:TokenValidityInMinutes não definido no appsettings."))
            );
            _tokenHandler = tokenHandler ?? new JwtSecurityTokenHandler();
        }

        /// <summary>
        /// Gera um token JWT com as informações do utilizador.
        /// </summary>
        public string GenerateToken(int userId, string email, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _settings.Issuer,
                _settings.Audience,
                claims,
                expires: DateTime.Now.AddMinutes(_settings.ExpireMinutes),
                signingCredentials: creds
            );

            return _tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Retorna a data de expiração de um token JWT.
        /// </summary>
        public DateTime GetTokenExpiration(string token)
        {
            var jwtToken = _tokenHandler.ReadToken(token) as JwtSecurityToken;
            return jwtToken?.ValidTo ?? throw new ArgumentException("Token inválido.");
        }

        /// <summary>
        /// Gera um refresh token aleatório.
        /// </summary>
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        /// <summary>
        /// Obtém as claims de um token JWT expirado.
        /// </summary>
        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false // Permite validar tokens expirados
            };

            try
            {
                var principal = _tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

                if (securityToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new SecurityTokenException("Token inválido.");
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        public ClaimsPrincipal? ValidateAccessToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_key); // mesma chave usada na geração

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out _);

                return principal;
            }
            catch
            {
                return null;
            }
        }


    }

    public static class ClaimsPrincipalExtensions
    {
        public static int? ObterUserId(this ClaimsPrincipal principal)
        {
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId)
                ? userId
                : null;
        }
    }

    /// <summary>
    /// Classe para armazenar as configurações JWT.
    /// </summary>
    public readonly record struct JwtSettings(string Key, string Issuer, string Audience, int ExpireMinutes);
}
