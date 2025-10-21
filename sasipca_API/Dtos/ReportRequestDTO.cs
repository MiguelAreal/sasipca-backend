using System.ComponentModel.DataAnnotations;
using static sasipca_API.Enumerators.Enums;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// Payload para solicitar a geração de um relatório.
    /// </summary>
    public class ReportRequestDTO
    {
        [Required(ErrorMessage = "O tipo de relatório é obrigatório.")]
        public ReportTypesEnum Type { get; set; }

        [Required(ErrorMessage = "O formato de saída é obrigatório.")]
        public ReportFormat Format { get; set; }

        [Required(ErrorMessage = "O nome do ficheiro é obrigatório e será usado para torná-lo único.")]
        [MaxLength(100)]
        public string FileName { get; set; } = null!;

        /// <summary>
        /// Parâmetros de filtro específicos para cada tipo de relatório (optional).
        /// </summary>
        public ReportFiltersDTO? Filters { get; set; }

        /// <summary>
        /// Necessário apenas para o tipo de relatório MovementDetails.
        /// </summary>
        public int? TargetMovementId { get; set; }
    }
}
