using Microsoft.EntityFrameworkCore;
using sasipca_API.DBModels;
using sasipca_API.Dtos;
using sasipca_API.Enumerators;
using sasipca_API.Services.Interfaces;

namespace sasipca_API.Services
{
    public class BeneficiaryService : IBeneficiaryService
    {
        private readonly SasipcaContext _dbcontext;

        public BeneficiaryService(SasipcaContext context)
        {
            _dbcontext = context;
        }

        public async Task<List<BeneficiaryListDTO>> GetBeneficiaries(string searchTerm)
        {
            var searchTermLower = searchTerm?.ToLower() ?? string.Empty;

            var beneficiaries = _dbcontext.Beneficiaries
                .Where(p => (string.IsNullOrEmpty(searchTerm) || p.Name.ToLower().Contains(searchTermLower)))
                .Select(p => new BeneficiaryListDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Email = p.Email
                });
            return await beneficiaries.ToListAsync();
        }
    }
}