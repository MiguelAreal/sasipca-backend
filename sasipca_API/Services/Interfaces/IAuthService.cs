using sasipca_API.DBModels;
using System.Threading.Tasks;

namespace sasipca_API.Services.Interfaces
{
    public interface IAuthService
    {
        Task<string?> ObterNome(int userId);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
        public int? GetUserId();
        Task<string> GerarOuManterRefreshToken(User user);
        Task<string> AtualizarRefreshTokenSeProximoExpirar(User user);
    }
}
