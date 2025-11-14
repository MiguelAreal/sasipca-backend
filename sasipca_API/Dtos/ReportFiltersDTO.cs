using sasipca_API.Enumerators;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// Filtros genéricos que suportam todos os relatórios.
    /// </summary>
    public class ReportFiltersDTO
    {
        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }
        public int? DeliveryStatus { get; set; } // Agendada, Entregue, Cancelada
        public int? BeneficiaryId { get; set; }
    }
}
