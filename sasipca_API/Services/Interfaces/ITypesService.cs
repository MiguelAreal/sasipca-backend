using sasipca_API.DBModels;
using System.Threading.Tasks;

namespace sasipca_API.Services.Interfaces
{
    public interface ITypesService
    {
        Task<bool> VerifyCategory(int categoryId);
        Task<bool> VerifyUnit(int unitId);
    }
}
