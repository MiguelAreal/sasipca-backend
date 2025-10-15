using System.Security.Claims;

namespace sasipca_API.Services.Interfaces
{
    public interface IJWTService
    {
        string GenerateToken(int userId, string email);
        DateTime GetTokenExpiration(string token);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
        ClaimsPrincipal? ValidateAccessToken(string token);
    }

}
