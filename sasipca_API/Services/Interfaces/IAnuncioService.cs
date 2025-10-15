using sasipca_API.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace sasipca_API.Services.Interfaces
{
    public interface IAnuncioService
    {
        Task<List<AnuncioListaDTO>> ObterAnuncios(string userPostalCode, string searchTerm, int? userId = null);
    }
}
