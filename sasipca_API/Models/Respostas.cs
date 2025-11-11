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
        public int? UserID { get; set; }
        public string UserName { get; set; }

        /// <summary>
        /// Inicialização do objeto AuthResponse
        /// </summary>
        /// <param name="token">Access Token</param>
        /// <param name="userId">ID do user</param>
        /// <param name="userName">Nome do user</param>
        public AuthResponse(string accesstoken,string refreshtoken,int userId,string userName)
        {
            AccessToken = accesstoken;
            RefreshToken = refreshtoken;
            UserName = userName;
            UserID = userId;
            
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