using sasipca_API.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace sasipca_API.Services.Interfaces
{
    public interface IBeneficiaryService
    {
        Task<List<BeneficiaryListDTO>> GetBeneficiaries(string searchTerm);
    }
}
