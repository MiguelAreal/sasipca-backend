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
    [Route("api/products")]
    [ApiController]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly SasipcaContext _dbContext;
        private readonly INotificationService _notifService;
        private readonly IAuthService _authService;
        private readonly ImageProcessingService _imageProcessingService;
        private readonly IProductService _productService;

        /// <summary>
        /// Inicialização do ProdutoController
        /// </summary>
        public ProductController(SasipcaContext context, INotificationService notifService, IAuthService authService, ImageProcessingService imageProcessingService, IProductService productService)
        {
            _dbContext = context;
            _notifService = notifService;
            _authService = authService;
            _imageProcessingService = imageProcessingService;
            _productService = productService;
        }

        /// <summary>
        /// Busca as categorias existentes de produtos
        /// </summary>
        /// <remarks>
        /// 
        /// Exemplo de resposta:
        /// {
        /// 
        ///    "Id": 1,
        ///    "Type": "Alimento"
        ///        
        /// }
        /// </remarks>
        /// <returns>Lista de categorias</returns>
        [HttpGet("categorias")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CategoriaProdutoGetDTO>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [Produces("application/json")]
        public async Task<ActionResult<List<CategoriaProdutoGetDTO>>> GetProductCategories()
        {
            try
            {
                var categorias = await _dbContext.CategoryTypes
                   .Select(p => new CategoriaProdutoGetDTO
                   {
                       Id = p.Id,
                       Type = p.Type,

                   }).ToListAsync();

                if (categorias == null || !categorias.Any())
                    return NotFound(new Resposta("Nenhuma categoria encontrada."));

                return Ok(categorias);
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao obter as categorias."));
            }
        }
    }
}