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
using sasipca_API.Data;
using sasipca_API.Dtos;
using sasipca_API.DBModels;

namespace sasipca_API.Controllers
{
    /// <summary>
    /// Controller para gestão de produtos.
    /// </summary>
    [Route("api/product")]
    [ApiController]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly SasipcaContext _dbContext;
        private readonly INotificacaoService _notifService;
        private readonly IAuthService _authService;
        private readonly AzureStorageService _storageService;
        private readonly ImageProcessingService _imageProcessingService;

        /// <summary>
        /// Inicialização do ProdutoController
        /// </summary>
        public ProductController(SasipcaContext context, INotificacaoService notifService, IAuthService authService, AzureStorageService storageService, ImageProcessingService imageProcessingService)
        {
            _dbContext = context;
            _notifService = notifService;
            _authService = authService;
            _storageService = storageService;
            _imageProcessingService = imageProcessingService;
        }

        /// <summary>
        /// Busca todos os detalhes de um produto específico.
        /// </summary>
        /// <param name="barcode">Código de barras / ID do produto</param>
        /// <returns>Detalhes do produto</returns>
        [HttpGet("{barcode}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductGetDTO))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [Produces("application/json")]
        public async Task<ActionResult<ProductGetDTO>> GetProduct(string barcode)
        {
            try
            {
                var userId = (int)HttpContext.Items["UserId"];

                var productDto = await _dbContext.Products
                .Where(p => p.Barcode == barcode)
                .Select(p => new ProductGetDTO
                {
                    Barcode = p.Barcode,
                    Name = p.Name,
                    Quantity = p.Quantity,
                    Category = p.Category,
                    Unit = p.Unit,
                    ProductLots = p.ProductLots.Select(lot => new ProductLotDTO
                    {
                        Id = lot.Id,
                        Lot = lot.Lot,
                        Quantity = lot.Quantity,
                        ExpiryDate = lot.ExpiryDate
                    }).ToList()
                })
                .FirstOrDefaultAsync();

                if (productDto == null)
                    return NotFound(new Resposta("Produto não encontrado."));

                return Ok(productDto);
            }
            catch (Exception)
            {
                return BadRequest(new Resposta("Ocorreu um erro ao obter o produto."));
            }
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
                var categorias = await _dbContext.CategoryTypes.ToListAsync();

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