using sasipca_API.Enumerators;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// Parâmetros de consulta para filtrar a lista de Entregas (Deliveries).
    /// </summary>
    public class DeliveryGetDTO
    {
        /// <summary>
        /// Filtra pelo ID do status (1: Agendada, 2: Entregue, 3: Cancelada).
        /// </summary>
        public Enums.DeliveryStatus? StatusId { get; set; }

        /// <summary>
        /// Filtra pelo ID do beneficiário.
        /// </summary>
        public int? BeneficiaryId { get; set; }

        /// <summary>
        /// Filtra entregas agendadas ou criadas a partir desta data (ex: 2025-01-01).
        /// </summary>
        public DateTime? DateFrom { get; set; }

        /// <summary>
        /// Filtra entregas agendadas ou criadas até esta data (ex: 2025-12-31).
        /// </summary>
        public DateTime? DateTo { get; set; }
    }
}
