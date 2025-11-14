using System.ComponentModel.DataAnnotations;
using Xunit.Sdk;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// Payload para AGENDAR uma nova Entrega de Stock a um Beneficiário.
    /// </summary>
    public class DeliveryPostDTO
    {
        [Required(ErrorMessage = "O ID do beneficiário é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "ID do beneficiário inválido.")]
        public int BeneficiaryId { get; set; }

        /// <summary>
        /// A data em que a entrega está agendada para ocorrer.
        /// Apenas necessário estar preenchido para quando é preciso agendar.
        /// </summary>
        public DateOnly ScheduledDate { get; set; }

        /// <summary>
        /// Notas ou observações sobre a entrega (opcional).
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Lista de produtos/lotes e quantidades a serem entregues.
        /// </summary>
        [Required(ErrorMessage = "A lista de Lotes a sair é obrigatória.")]
        [MinLength(1, ErrorMessage = "A lista de Lotes a sair deve conter pelo menos um item.")]
        public List<DeliveryItemDTO> ItemsToDeliver { get; set; } = new List<DeliveryItemDTO>();
    }
}
