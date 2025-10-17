using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// Classe base de Data Transfer Object para lista beneficiários.
    /// </summary>
    public class BeneficiaryListDTO
    {
        /// <summary>
        /// Identificador do beneficiário
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome do beneficiário
        /// </summary>
        [Required]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Email do beneficiário
        /// </summary>
        public string? Email { get; set; }


    }
}
