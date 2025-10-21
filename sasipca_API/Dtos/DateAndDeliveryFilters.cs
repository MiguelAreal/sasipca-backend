using sasipca_API.Enumerators;

namespace sasipca_API.Dtos
{
    // A classe que será usada no ReportingService (assumindo que foi renomeada ou herdada do DeliveryQueryDTO)
    public class DateAndDeliveryFiltersDTO : ReportFiltersDTO
    {
        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }
        public int? StatusId { get; set; }
        public int? BeneficiaryId { get; set; }
    }

}
