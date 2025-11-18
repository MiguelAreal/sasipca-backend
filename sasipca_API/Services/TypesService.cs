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
    public class TypesService : ITypesService
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
        public TypesService(SasipcaContext dbcontext, IJWTService jwtService, IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _dbcontext = dbcontext;
            _jwtService = jwtService;
            _refreshTokenValidityMinutes = int.Parse(config["Jwt:RefreshTokenValidityInMinutes"] ?? "7200");
            _httpContextAccessor = httpContextAccessor;
        }


        /// <summary>
        /// Método para verificar se a categoria existe.
        /// </summary>
        /// <param name="categoryId">ID da categoria para verificação.</param>
        /// <returns>Retorna um valor booleano indicando se a categoria existe.</returns>
        public async Task<bool> VerifyCategory(int categoryId)
        {
            return await _dbcontext.CategoryTypes
                .AnyAsync(c => c.Id == categoryId);
        }

        /// <summary>
        /// Método para verificar se o tipo de unidade existe.
        /// </summary>
        /// <param name="unitId">ID do tipo de unidade para verificação.</param>
        /// <returns>Retorna um valor booleano indicando se a unidade existe.</returns>
        public async Task<bool> VerifyUnit(int unitId)
        {
            return await _dbcontext.UnitTypes
                .AnyAsync(c => c.Id == unitId);
        }
    }
}
