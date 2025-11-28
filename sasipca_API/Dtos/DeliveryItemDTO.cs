using System.ComponentModel.DataAnnotations;
using Xunit.Sdk;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// Item da lista de produtos/grupos a serem entregues.
    /// </summary>
    public class DeliveryItemDTO
    {
        /// <summary>
        /// Barcode do produto que está a ser entregue.
        /// </summary>
        [Required(ErrorMessage = "Barcode é obrigatório.")]
        public string Barcode { get; set; } = null!;

        /// <summary>
        /// ID do grupo específico do produto.
        /// </summary>
        [Required(ErrorMessage = "ID do grupo obrigatório")]
        public int groupId { get; set; }

        /// <summary>
        /// Quantidade a sair do lote.
        /// </summary>
        [Required(ErrorMessage = "A Quantidade é obrigatória.")]
        [Range(1, 9999, ErrorMessage = "A quantidade deve ser positiva.")]
        public int Quantity { get; set; }
    }
}
