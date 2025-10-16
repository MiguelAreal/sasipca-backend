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
    [Route("api/products")]
    [ApiController]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly SasipcaContext _dbContext;
        private readonly INotificacaoService _notifService;
        private readonly IAuthService _authService;
        private readonly AzureStorageService _storageService;
        private readonly ImageProcessingService _imageProcessingService;
        private readonly IProductService _productService;

        /// <summary>
        /// Inicialização do ProdutoController
        /// </summary>
        public ProductController(SasipcaContext context, INotificacaoService notifService, IAuthService authService, AzureStorageService storageService, ImageProcessingService imageProcessingService, IProductService productService)
        {
            _dbContext = context;
            _notifService = notifService;
            _authService = authService;
            _storageService = storageService;
            _imageProcessingService = imageProcessingService;
            _productService = productService;
        }


        /// <summary>
        /// Busca todos os produtos existentes consoante filtros.
        /// </summary>
        /// <remarks>
        /// <param name="pageNumber">Número da página (começa em 1)</param>
        /// <param name="pageSize">Quantidade de itens por página (máx. 50)</param>
        /// <param name="orderBy">Ordenação Alfabética ("asc" = Ascendente, "desc" = Descendente</param>
        /// <param name="searchTerm">Termo para busca por nome</param>
        /// <returns>Lista paginada de anúncios</returns>
        [HttpGet()]
        public async Task<ActionResult<PaginatedResponse<ProductListDTO>>> GetAnuncios(
             [FromQuery] int pageNumber = 1,
             [FromQuery] int pageSize = 10,
             [FromQuery] string orderBy = "asc",
             [FromQuery] string searchTerm = "")
        {
            try
            {
                // Validação dos parâmetros
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 10;
                if (orderBy != "asc" && orderBy != "desc")
                    return BadRequest(new Resposta("Parâmetro orderBy deve ser 'asc' ou 'desc'"));

                var products = await _productService.GetProducts(searchTerm);

                products = orderBy == "desc"
                    ? products.OrderByDescending(a => a.Name).ToList()
                    : products.OrderBy(a => a.Name).ToList();

                if (!products.Any())
                {
                    return NotFound(new Resposta("Nenhum produto encontrado com os filtros e termo de pesquisa."));
                }

                // Aplica paginação
                var totalCount = products.Count;
                var pagedProducts = products
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Cria resposta paginada
                var paginatedResponse = new PaginatedResponse<ProductListDTO>
                {
                    Data = pagedProducts,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                if (!paginatedResponse.Data.Any())
                {
                    return NotFound(new Resposta("Página solicitada está vazia."));
                }

                return Ok(paginatedResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                                  new Resposta("Ocorreu um erro interno ao obter os anúncios."));
            }
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
                var productDto = await _dbContext.Products
                    .Include(p => p.ProductLots)
                    .Include(p => p.Category)
                    .Include(p => p.Unit)
                    .Where(p => p.Barcode == barcode)
                    .Select(p => new ProductGetDTO
                    {
                        Barcode = p.Barcode,
                        Name = p.Name,
                        Quantity = p.Quantity,
                        Category = p.Category.Type,
                        Unit = p.Unit.Type,
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
            catch (Exception ex)
            {
                // Idealmente, deve registar a exceção 'ex' aqui
                return StatusCode(StatusCodes.Status500InternalServerError,
                                  new Resposta("Ocorreu um erro interno ao obter o produto."));
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