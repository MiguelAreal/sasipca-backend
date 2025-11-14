using sasipca_API.Enumerators;
using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// Payload para atualizar uma Entrega existente (data e/ou itens e/ou status).
    /// </summary>
    public class DeliveryUpdateDTO
    {
        /// <summary>
        /// A nova data em que a entrega está agendada.
        /// </summary>
        public DateOnly? ScheduledDate { get; set; }

        /// <summary>
        /// O novo status da entrega (1: Agendada, 2: Entregue, 3: Cancelada).
        /// </summary>
        public int NewStatusId { get; set; }

        /// <summary>
        /// Notas ou observações sobre a alteração (opcional).
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// A lista COMPLETA de produtos/lotes a serem entregues (substitui a lista anterior).
        /// </summary>
        [Required(ErrorMessage = "A lista de Itens é obrigatória para atualização, mesmo que vazia.")]
        public List<DeliveryItemDTO> ItemsToDeliver { get; set; } = new List<DeliveryItemDTO>();
    }
}
