using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sasipca_API.Models;
using Microsoft.AspNetCore.Authorization;
using sasipca_API.Services;
using sasipca_API.Services.Interfaces;
using sasipca_API.Enumerators;
using sasipca_API.Dtos;
using sasipca_API.DBModels;

namespace sasipca_API.Controllers
{
    /// <summary>
    /// Controller para gestão de produtos.
    /// </summary>
    [Route("api/lists")]
    [ApiController]
    public class ListController : ControllerBase
    {
        private readonly SasipcaContext _dbContext;
        private readonly INotificationService _notifService;
        private readonly IAuthService _authService;
        private readonly ImageProcessingService _imageProcessingService;
        private readonly IProductService _productService;

        /// <summary>
        /// Inicialização do ProdutoController
        /// </summary>
        public ListController(SasipcaContext context)
        {
            _dbContext = context;
        }

        /// <returns>Lista de categorias</returns>
        /// <summary>
        /// Busca as listas de categorias e tipos de unidade para produtos.
        /// </summary>
        /// <returns>Objeto contendo as listas.</returns>
        [HttpGet()]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ListsGetDTO))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [Produces("application/json")]
        public async Task<ActionResult<ListsGetDTO>> GetProductLists()
        {
            try
            {
                // Busca categorias
                var categorias = await _dbContext.CategoryTypes
                    .Select(c => new CategoriesGetDTO
                    {
                        Id = c.Id,
                        Type = c.Type
                    }).ToListAsync();

                // Busca tipos de unidade
                var tipos = await _dbContext.UnitTypes
                    .Select(u => new UnitTypesGetDTO
                    {
                        Id = u.Id,
                        Type = u.Type
                    }).ToListAsync();

                if ((!categorias.Any()) && (!tipos.Any()))
                    return NotFound(new Resposta("Nenhuma lista encontrada."));

                var result = new ListsGetDTO
                {
                    Categories = categorias,
                    Types = tipos
                };

                return Ok(result);
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao obter as listas."));
            }
        }

    } 
}