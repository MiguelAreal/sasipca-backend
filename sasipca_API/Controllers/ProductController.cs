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
        private readonly ITypesService _typesService;

        /// <summary>
        /// Inicialização do ProdutoController
        /// </summary>
        public ProductController(SasipcaContext context, INotificationService notifService, IAuthService authService, ImageProcessingService imageProcessingService, IProductService productService, ITypesService typesService)
        {
            _dbContext = context;
            _notifService = notifService;
            _authService = authService;
            _imageProcessingService = imageProcessingService;
            _productService = productService;
            _typesService = typesService;
            
        }



        /// <summary>
        /// Busca dados de todos os produtos existentes com quantidades reais (View) consoante filtros.
        /// </summary>
        /// <remarks>
        /// <param name="pageNumber">Número da página (começa em 1)</param>
        /// <param name="pageSize">Quantidade de itens por página (máx. 50)</param>
        /// <param name="orderBy">Ordenação Alfabética ("asc" = Ascendente, "desc" = Descendente</param>
        /// <param name="searchTerm">Termo para busca por nome</param>
        /// <returns>Lista paginada de produtos</returns>
        [HttpGet()]
        public async Task<ActionResult<PaginatedResponse<ProductListDTO>>> GetAllProducts(
         [FromQuery] int pageNumber = 1,
         [FromQuery] int pageSize = 10,
         [FromQuery] string orderBy = "asc",
         [FromQuery] string searchTerm = "")
        {
            try
            {
                // Validação de parâmetros
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 10;
                if (orderBy != "asc" && orderBy != "desc")
                    return BadRequest(new Resposta("Parâmetro orderBy deve ser 'asc' ou 'desc'"));

                var searchTermLower = searchTerm?.ToLower() ?? string.Empty;

                // Consulta à view
                var query = _dbContext.VStockPerProducts.AsQueryable();
                if (!string.IsNullOrEmpty(searchTerm))
                    query = query.Where(p => p.Name.ToLower().Contains(searchTermLower));

                // Projeção
                var products = query
                    .Select(p => new ProductListDTO
                    {
                        Barcode = p.Barcode,
                        Name = p.Name,
                        CategoryId = p.CategoryId,
                        UnitId = p.UnitId,
                        UnitSize = p.UnitSize,
                        TotalQuantity = (int)p.TotalQuantity,
                        ReservedQuantity = (int)p.ReservedQuantity,
                        AvailableStock = (int)p.AvailableStock
                    });

                // Ordenação
                products = orderBy == "desc"
                    ? products.OrderByDescending(p => p.Name)
                    : products.OrderBy(p => p.Name);

                // Paginação
                var totalCount = await products.CountAsync();
                var pagedProducts = await products
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                if (!pagedProducts.Any())
                    return NotFound(new Resposta("Nenhum produto encontrado."));

                var paginatedResponse = new PaginatedResponse<ProductListDTO>
                {
                    Data = pagedProducts,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return Ok(paginatedResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new Resposta($"Ocorreu um erro interno ao obter os produtos: {ex.Message}"));
            }
        }


        /// <summary>
        /// Busca todos os detalhes de um produto específico. Inclui lotes e quantidades reais.
        /// </summary>
        /// <param name="barcode">Código de barras / ID do produto</param>
        /// <returns>Detalhes do produto</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductGetDTO))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [Produces("application/json")]
        [HttpGet("{barcode}")]
        public async Task<ActionResult<ProductGetDTO>> GetProduct(string barcode)
        {
            try
            {
                // 1. Busca os dados da View para o produto específico (traz todos os lotes)
                var stockData = await _dbContext.VStockPerGroups
                    .Where(v => v.Barcode == barcode)
                    .ToListAsync();

                if (!stockData.Any())
                {
                    // Se não houver dados de stock, verifica-se se o produto existe na tabela Products
                    var productExists = await _dbContext.Products
                        .AnyAsync(p => p.Barcode == barcode);

                    if (!productExists)
                        return NotFound(new Resposta("Produto não encontrado."));
                }

                // 2. Agregação: Calcula os totais do produto (soma de todos os lotes)
                var totalStock = stockData.GroupBy(v => v.Barcode).Select(g => new
                {
                    TotalQuantity = g.Sum(x => x.TotalQuantity),
                    ReservedQuantity = g.Sum(x => x.ReservedQuantity),
                    AvailableStock = g.Sum(x => x.AvailableStock),
                    // Trazemos os campos do produto uma vez
                    Name = g.First().Name,
                    UnitSize = g.First().UnitSize,
                    CategoryId = g.First().CategoryId,
                    UnitId = g.First().UnitId
                }).FirstOrDefault();

                if (totalStock == null)
                {
                    // O produto existe, mas não tem lotes. Montar um DTO vazio.
                    var simpleProduct = await _dbContext.Products
                        .Include(p => p.Category)
                        .Include(p => p.Unit)
                        .Where(p => p.Barcode == barcode)
                        .Select(p => new ProductGetDTO
                        {
                            Barcode = p.Barcode,
                            Name = p.Name,
                            UnitSize = p.UnitSize,
                            CategoryId = p.Category.Id,
                            UnitId = p.Unit.Id,
                            ProductGroups = new List<ProductGroupDTO>(),
                            TotalQuantity = 0,
                            ReservedQuantity = 0,
                            AvailableStock = 0
                        }).FirstOrDefaultAsync();

                    return Ok(simpleProduct);
                }

                // 3. Mapeamento: Cria a lista de grupos (ProductGroups) e o DTO principal.
                var productGroupsDto = stockData.Select(groupData => new ProductGroupDTO
                {
                    Id = groupData.ProductGroupId,
                    ExpiryDate = groupData.ExpiryDate,
                    TotalQuantity = groupData.TotalQuantity,
                    ReservedQuantity = (int)groupData.ReservedQuantity, // Cast necessário
                    AvailableStock = (int)groupData.AvailableStock      // Cast necessário
                }).ToList();

                var productDto = new ProductGetDTO
                {
                    Barcode = barcode,
                    Name = totalStock.Name,
                    UnitSize = totalStock.UnitSize,
                    CategoryId = totalStock.CategoryId,
                    UnitId = totalStock.UnitId,

                    // Totais Agregados
                    TotalQuantity = totalStock.TotalQuantity,
                    ReservedQuantity = (int)totalStock.ReservedQuantity,
                    AvailableStock = (int)totalStock.AvailableStock,

                    // Lista Aninhada
                    ProductGroups= productGroupsDto
                };

                return Ok(productDto);
            }
            catch (Exception ex)
            {
                // Idealmente, deve registar a exceção 'ex' aqui
                return StatusCode(StatusCodes.Status500InternalServerError,
                                    new Resposta("Ocorreu um erro interno ao obter o produto."));
            }
        }



        // ----------------------------------------------------
        // ATUALIZAÇÃO DE PRODUTO (PUT)
        // ----------------------------------------------------
        /// <summary>
        /// Atualiza os dados de cabeçalho de um produto.
        /// Apenas Nome, Categoria, Quantidade unitária e Tipo de Quantidade
        /// </summary>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Resposta))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(Resposta))]
        [HttpPut("{barcode}")]
        public async Task<ActionResult> PutProduct(string barcode, [FromBody] ProductPutDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var product = await _dbContext.Products.FindAsync(barcode);
            if (product == null) return NotFound(new Resposta($"Produto {barcode} não encontrado."));


            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Nome
                if (!string.IsNullOrWhiteSpace(dto.Name))
                    product.Name = dto.Name;

                // UnitSize
                if (dto.UnitSize.HasValue) product.UnitSize = dto.UnitSize.Value;
                

                // Validação de tipo de unidade
                if (dto.UnitId.HasValue)
                {
                    if (!(await _typesService.VerifyUnit(dto.UnitId.Value)))
                    {
                        throw new Exception("A unidade informada não existe.");
                    }
                    else
                    {
                        product.UnitId = dto.UnitId.Value;
                    }
                }

                // Validação de categoria
                if (dto.CategoryId.HasValue)
                {
                    if (!(await _typesService.VerifyCategory(dto.CategoryId.Value)))
                    {
                        throw new Exception("A categoria informada não existe.");
                    }
                    else
                    {
                        product.CategoryId = dto.CategoryId.Value;
                    }
                }

                // Validação de Quantidade
                if (dto.UnitSize.HasValue)
                {
                    product.UnitSize = dto.UnitSize.Value;
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new Resposta("Produto atualizado com sucesso."));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, new Resposta("Erro ao atualizar o produto."));
            }
        }


    } 
}