using sasipca_API.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace sasipca_API.Services.Interfaces
{
    public interface IProductService
    {
        Task<List<ProductListDTO>> GetProducts(string searchTerm);
    }
}
