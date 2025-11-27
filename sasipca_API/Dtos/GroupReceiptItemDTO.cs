using System.ComponentModel.DataAnnotations;
using Xunit.Sdk;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// Detalhes de cada lote a ser criado ou atualizado numa entrada de stock.
    /// </summary>
    public class GroupReceiptItemDTO
    {
        /// <summary>
        /// Quantidade a ser adicionada a este grupo.
        /// </summary>
        [Required(ErrorMessage = "A Quantidade é obrigatória.")]
        [Range(1, int.MaxValue, ErrorMessage = "A Quantidade deve ser positiva.")]
        public int Quantity { get; set; }

        /// <summary>
        /// Data de validade deste grupo (obrigatório para novos grupos).
        /// </summary>
        [Required(ErrorMessage = "A Data de Validade é obrigatória.")]
        public DateOnly ExpiryDate { get; set; }
    }
}
