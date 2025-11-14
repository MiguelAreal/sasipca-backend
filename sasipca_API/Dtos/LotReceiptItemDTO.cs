using System.ComponentModel.DataAnnotations;
using Xunit.Sdk;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// Detalhes de cada lote a ser criado ou atualizado numa entrada de stock.
    /// </summary>
    public class LotReceiptItemDTO
    {
        /// <summary>
        /// Identificador do lote (ex: Lote A, Lote B). Se existir, será atualizado; caso contrário, será criado.
        /// </summary>
        [Required(ErrorMessage = "O identificador do Lote é obrigatório.")]
        public string Lot { get; set; } = null!;

        /// <summary>
        /// Quantidade a ser adicionada a este lote.
        /// </summary>
        [Required(ErrorMessage = "A Quantidade é obrigatória.")]
        [Range(1, int.MaxValue, ErrorMessage = "A Quantidade deve ser positiva.")]
        public int Quantity { get; set; }

        /// <summary>
        /// Data de validade deste lote (obrigatório para novos lotes).
        /// </summary>
        [Required(ErrorMessage = "A Data de Validade é obrigatória.")]
        public DateTime ExpiryDate { get; set; }
    }
}
