using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{

    /// <summary>
    /// DTO usado para criar uma nova proposta
    /// </summary>
    public class PropostaProdutoPostDTO
    {
        /// <summary>
        /// Valor dado pelo comprador.
        /// </summary>
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
        public decimal Valor { get; set; }
    }
}
