using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sasipca_API.Models;
using Microsoft.AspNetCore.Authorization;
using sasipca_API.Enumerators;
using sasipca_API.Services;
using sasipca_API.Data;
using sasipca_API.Dtos;
using Azure;
using Microsoft.IdentityModel.Tokens;
using sasipca_API.Services.Interfaces;

namespace sasipca_API.Controllers
{
    /// <summary>
    /// Controller para gestão de anúncios
    /// </summary>
    [Route("api/anuncios")]
    [ApiController]
    [Authorize]
    public class AnuncioController : ControllerBase
    {
        private readonly NLDbContext _dbcontext;
        private readonly IAuthService _authService;
        private readonly IAnuncioService _anuncioService;

        /// <summary>
        /// Inicialização do AnuncioController
        /// </summary>
        /// <param name="context">Contexto da base de dados</param>
        /// <param name="authService">Serviço de autenticação</param>
        public AnuncioController(NLDbContext context, IAuthService authService, IAnuncioService anuncioService)
        {
            _dbcontext = context;
            _authService = authService;
            _anuncioService = anuncioService;
        }

        /// <summary>
        /// Busca todos os anúncios em estado 'Criado' com paginação, ordenação e filtros
        /// </summary>
        /// <remarks>
        /// Retorna apenas anúncios que partilham o código postal com o utilizador autenticado.
        /// 
        /// </remarks>
        /// <param name="pageNumber">Número da página (começa em 1)</param>
        /// <param name="pageSize">Quantidade de itens por página (máx. 50)</param>
        /// <param name="orderBy">Ordenação ("newest" = mais recente primeiro, "oldest" = mais antigo primeiro)</param>
        /// <param name="searchTerm">Termo para busca por nome</param>
        /// <param name="userId">Filtro por criador do anúncio.</param>
        /// <returns>Lista paginada de anúncios</returns>
        /*[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<AnuncioListaDTO>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Response))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Response))]
        [Produces("application/json")]
        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<AnuncioListaDTO>>> GetAnuncios(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string orderBy = "newest",
            [FromQuery] string searchTerm = "",
            [FromQuery] int? userId = null) 
        {
            try
            {
                // Validação dos parâmetros
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 10;
                if (orderBy != "newest" && orderBy != "oldest")
                    return BadRequest(new Resposta("Parâmetro orderBy deve ser 'newest' ou 'oldest'"));

                // Busca ID da pessoa autenticada
                var currentuserId = (int)HttpContext.Items["UserId"];

                // Busca código postal da pessoa autenticada
                var userPostalCode = "5555";
                if (userPostalCode == null)
                    return Unauthorized(new Resposta("Não foi possível obter o código postal do utilizador."));

                // Obtém todos os anúncios com filtros aplicados
                var anuncios = await _anuncioService.ObterAnuncios(userPostalCode, searchTerm, userId);

                // Aplica ordenação
                anuncios = orderBy == "newest"
                    ? anuncios.OrderByDescending(a => a.DataCriacao).ToList()
                    : anuncios.OrderBy(a => a.DataCriacao).ToList();

                // Aplica paginação
                var totalCount = anuncios.Count;
                var pagedAnuncios = anuncios
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Cria resposta paginada
                var paginatedResponse = new PaginatedResponse<AnuncioListaDTO>
                {
                    Data = pagedAnuncios,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                if (paginatedResponse.Data.IsNullOrEmpty()) return NotFound(new Resposta("Nenhum anúncio encontrado com os filtros selecionados."));

                return Ok(paginatedResponse);
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao obter os anúncios."));
            }
        }*/


    }
}