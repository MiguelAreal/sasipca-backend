using System;
using System.Threading.Tasks;

namespace sasipca_API.Services.Interfaces
{
    public interface IJobSchedulerService
    {
        void ScheduleDeliveryCheck(int deliveryId, DateOnly scheduledDate);
        Task VerifyDeliveryStatus(int deliveryId, DateOnly expectedDate);
    }
}
