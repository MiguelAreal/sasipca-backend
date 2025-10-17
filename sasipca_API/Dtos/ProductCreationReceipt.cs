using System.ComponentModel.DataAnnotations;
namespace sasipca_API.Dtos
{
    
    /// <summary>
    /// Payload para criar um Produto E registar a sua primeira entrada de Stock.
    /// Exige dados completos do Produto Mestre e do Lote.
    /// </summary>
    public class ProductCreationReceiptDTO
    {
        // --- Dados do Produto Mestre ---
        [Required(ErrorMessage = "O Barcode é obrigatório.")]
        public string Barcode { get; set; } = null!;

        [Required(ErrorMessage = "O Nome do produto é obrigatório.")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "O ID da Categoria é obrigatório.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "O ID da Unidade é obrigatório.")]
        public int UnitId { get; set; }

        /// <summary>
        /// Quantidade por unidade (ex: 1kg, 250ml). Valor padrão 1.
        /// </summary>
        public int UnitSize { get; set; } = 1;

        // --- Dados do Lote Inicial ---
        [Required(ErrorMessage = "Os dados do Lote inicial são obrigatórios para a criação do produto.")]
        public LotReceiptItemDTO InitialLot { get; set; } = null!;

        /// <summary>
        /// Notas ou observações sobre a entrada (opcional).
        /// </summary>
        public string? Note { get; set; }
    }
}
