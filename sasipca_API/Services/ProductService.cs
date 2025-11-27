using Microsoft.EntityFrameworkCore;
using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Services.Interfaces;

namespace sasipca_API.Services
{
    public class ProductService : IProductService
    {
        private readonly SasipcaContext _dbcontext;

        public ProductService(SasipcaContext context)
        {
            _dbcontext = context;
        }

        public async Task<List<ProductListDTO>> GetAllProducts(string searchTerm)
        {
            var searchTermLower = searchTerm?.ToLower() ?? string.Empty;

            var totalStockData = await _dbcontext.VStockPerGroups
                .GroupBy(v => v.Barcode)
                .Select(g => new
                {
                    Barcode = g.Key,
                    TotalQuantity = g.Sum(x => x.TotalQuantity),
                    ReservedQuantity = g.Sum(x => x.ReservedQuantity),
                    AvailableStock = g.Sum(x => x.AvailableStock)
                })
                .ToListAsync(); // Executar a agregação primeiro na DB

            // Step 2: Query Products and perform a LEFT JOIN (GroupJoin) IN MEMORY.
            // O GroupJoin (e FirstOrDefault) agora é feito sobre uma lista em memória.
            var productsQuery = _dbcontext.Products
                .Where(p => (string.IsNullOrEmpty(searchTerm) || p.Name.ToLower().Contains(searchTermLower)))
                .Include(p => p.Category)
                .Include(p => p.Unit)
                .ToList() // <--- O GroupJoin será feito sobre esta lista em memória
                .GroupJoin(
                    totalStockData,
                    product => product.Barcode,
                    stock => stock.Barcode,
                    (product, stockGroup) => new { Product = product, Stock = stockGroup.FirstOrDefault() }
                )
                // O restante da projeção é o mesmo
                .Select(p => new ProductListDTO
                {
                    Barcode = p.Product.Barcode,
                    Name = p.Product.Name,
                    UnitId = p.Product.Unit.Id,
                    CategoryId = p.Product.Category.Id,
                    TotalQuantity = p.Stock != null ? p.Stock.TotalQuantity : 0,
                    ReservedQuantity = p.Stock != null ? (int)p.Stock.ReservedQuantity : 0,
                    AvailableStock = p.Stock != null ? (int)p.Stock.AvailableStock : 0
                });

            // Retorna o resultado que já está em memória (não precisa de .ToListAsync() no final)
            return productsQuery.ToList();
        }

        public async Task<List<ProductListDTO>> GetProduct(string searchTerm)
        {
            var searchTermLower = searchTerm?.ToLower() ?? string.Empty;

            // Agregar dados de stock da view VAvailableStockPerLot
            var totalStockData = _dbcontext.VStockPerGroups
                .GroupBy(v => v.Barcode)
                .Select(g => new
                {
                    Barcode = g.Key,
                    TotalQuantity = g.Sum(x => x.TotalQuantity),
                    ReservedQuantity = g.Sum(x => x.ReservedQuantity),
                    AvailableStock = g.Sum(x => x.AvailableStock)
                });

            // Buscar produtos e fazer LeftJoin para dados agregados
            var products = _dbcontext.Products
                .Where(p => (string.IsNullOrEmpty(searchTerm) || p.Name.ToLower().Contains(searchTermLower)))
                .Include(p => p.Category)
                .Include(p => p.Unit)
                .GroupJoin(
                    totalStockData,
                    product => product.Barcode,
                    stock => stock.Barcode,
                    (product, stockGroup) => new { Product = product, Stock = stockGroup.FirstOrDefault() }
                )

                // Projetar resultados para ProductListDTO.
                .Select(p => new ProductListDTO
                {
                    Barcode = p.Product.Barcode,
                    Name = p.Product.Name,
                    UnitId = p.Product.Unit.Id,
                    CategoryId = p.Product.Category.Id,
                    TotalQuantity = p.Stock != null ? p.Stock.TotalQuantity : 0,
                    ReservedQuantity = p.Stock != null ? (int?)p.Stock.ReservedQuantity : 0, // Cast decimal to int?
                    AvailableStock = p.Stock != null ? (int?)p.Stock.AvailableStock : 0      // Cast decimal to int?
                });

            return await products.ToListAsync();
        }



    }
}