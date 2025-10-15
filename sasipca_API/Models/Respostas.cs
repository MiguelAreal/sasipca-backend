namespace sasipca_API.Models
{
    #region Classes de Resposta

    /// <summary>
    /// Resposta de autenticação.
    /// </summary>
    public class AuthResponse
    {
        public int? UserID { get; set; }
        public string Token { get; set; }

        /// <summary>
        /// Inicialização do objeto AuthResponse
        /// </summary>
        /// <param name="token">Access Token</param>
        /// <param name="userId">ID do user</param>
        public AuthResponse(string token,int userId)
        {
            UserID = userId;
            Token = token;
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