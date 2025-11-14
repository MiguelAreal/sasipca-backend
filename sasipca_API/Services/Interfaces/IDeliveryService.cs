using sasipca_API.Dtos;
using sasipca_API.Enumerators;
using sasipca_API.Models;
using System.Threading.Tasks;

namespace sasipca_API.Services.Interfaces
{
    public interface IDeliveryService
    {
        Task<(bool success, Resposta? response)> CreateDelivery(DeliveryPostDTO dto,int userId,Enums.DeliveryStatus initialStatus,bool deductStock);
        Task<(bool success, Resposta? response)> UpdateDelivery(int deliveryId,DeliveryUpdateDTO dto,int userId);
        Task<(bool success, Resposta? response)> DeleteDelivery(int deliveryId);
    }
}
