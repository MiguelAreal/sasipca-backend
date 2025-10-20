using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// Payload para registar um ajuste (correção) de stock num lote específico.
    /// </summary>
    public class StockAdjustmentDTO
    {
        /// <summary>
        /// Código de barras do produto.
        /// </summary>
        [Required(ErrorMessage = "O Barcode é obrigatório.")]
        public string Barcode { get; set; } = null!;

        /// <summary>
        /// Identificador do lote a ser ajustado.
        /// </summary>
        [Required(ErrorMessage = "O Lote é obrigatório.")]
        public string Lot { get; set; } = null!;

        /// <summary>
        /// Quantidade a ser adicionada (valor positivo) ou removida (valor negativo) do stock.
        /// Não pode ser zero.
        /// </summary>
        [Required(ErrorMessage = "A Quantidade de ajuste é obrigatória.")]
        [Range(-99999, 99999, ErrorMessage = "A quantidade é obrigatória.")]
        public int QuantityAdjustment { get; set; }

        /// <summary>
        /// Notas/Justificativa do ajuste (obrigatório).
        /// </summary>
        [Required(ErrorMessage = "A justificação do ajuste é obrigatória.")]
        public string Note { get; set; } = null!;
    }
}
