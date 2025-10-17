using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// Payload principal para o registo de uma Entrada de Stock (Comando).
    /// </summary>
    public class StockReceiptDTO
    {
        /// <summary>
        /// Notas ou observações sobre a entrada (opcional).
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Identificador do Produto (Barcode)
        /// </summary>
        [Required(ErrorMessage = "Barcode é obrigatório")]
        public string Barcode { get; set; } = null!;

        /// <summary>
        /// Lista de Lotes e Quantidades a serem adicionadas.
        /// </summary>
        [Required(ErrorMessage = "A lista de Itens de Lote é obrigatória.")]
        public List<LotReceiptItemDTO> LotsToEnter { get; set; } = new List<LotReceiptItemDTO>();
    }
}
