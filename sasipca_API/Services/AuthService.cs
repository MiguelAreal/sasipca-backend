using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sasipca_API.Data;
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
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SasipcaContext _dbcontext;

        /// <summary>
        /// Construtor que inicializa o serviço com o IHttpContextAccessor e o contexto da base de dados.
        /// </summary>
        /// <param name="httpContextAccessor">Provedor de contexto HTTP para acessar informações sobre a requisição.</param>
        /// <param name="dbcontext">Contexto da base de dados para interagir com os dados de utilizadores e outros.</param>
        public AuthService(IHttpContextAccessor httpContextAccessor, SasipcaContext dbcontext)
        {
            _httpContextAccessor = httpContextAccessor;
            _dbcontext = dbcontext;
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
    }
}
