using NuGet.Common;

namespace sasipca_API.Models
{
    #region Classes de Resposta

    /// <summary>
    /// Resposta de autenticação.
    /// </summary>
    public class AuthResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int UserID { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; }

        public AuthResponse(string accessToken, string refreshToken, int userId, string userName, string role)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            UserID = userId;
            UserName = userName;
            Role = role;
        }
    }

    /// <summary>
    /// Classe de Resposta genérica.
    /// </summary>
    public class Resposta
    {
        /// <summary>
        /// Mensagem dada na resposta.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Inicialização do objeto resposta.
        /// </summary>
        /// <param name="message">Mensagem a ser passada.</param>
        public Resposta(string message)
        {
            Message = message;
        }
    }

    #endregion
}