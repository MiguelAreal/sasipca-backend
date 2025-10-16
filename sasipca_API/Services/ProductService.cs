// Services/AnuncioService.cs
using Microsoft.EntityFrameworkCore;
using sasipca_API.Data;
using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Enumerators;
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

        public async Task<List<ProductListDTO>> GetProducts(string searchTerm)
        {
            var searchTermLower = searchTerm?.ToLower() ?? string.Empty;

            var products = _dbcontext.Products
                .Where(p => (string.IsNullOrEmpty(searchTerm) || p.Name.ToLower().Contains(searchTermLower)))
                .Include(p => p.Category)
                .Include(p => p.Unit)
                .Select(p => new ProductListDTO
                {
                    Barcode = p.Barcode,
                    Name = p.Name,
                    Unit = p.Unit.Type,
                    Category = p.Category.Type
                });
            return await products.ToListAsync();
        }
    }
}