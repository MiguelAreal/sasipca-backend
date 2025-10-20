using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// Payload unificado para registar Entrada de Stock.
    /// Pode criar um novo Produto (se 'Name', 'CategoryId', etc. forem fornecidos e o Barcode for novo)
    /// ou adicionar stock a um Produto existente (apenas 'Barcode' e 'LotsToEnter' são necessários).
    /// </summary>
    public class StockReceiptDTO
    {
        // --- Campos de Identificação/Stock (Sempre Obrigatórios) ---
        [Required(ErrorMessage = "O Barcode é obrigatório.")]
        public string Barcode { get; set; } = null!;

        [Required(ErrorMessage = "A lista de Itens de Lote é obrigatória e deve ter pelo menos um item.")]
        [MinLength(1, ErrorMessage = "A lista de Itens de Lote deve conter pelo menos um item.")]
        public List<LotReceiptItemDTO> LotsToEnter { get; set; } = new List<LotReceiptItemDTO>();

        // --- Campos de Criação do Produto (Opcionais, mas necessários se o produto não existir) ---
        // Se o produto não existir, a validação de negócio irá exigir estes campos.
        public string? Name { get; set; }
        public int? CategoryId { get; set; }
        public int? UnitId { get; set; }

        /// <summary>
        /// Quantidade por unidade (ex: 1kg, 250ml). Valor padrão 1.
        /// </summary>
        public int UnitSize { get; set; } = 1;

        // --- Campo de Movimentação (Opcional) ---
        /// <summary>
        /// Notas ou observações sobre a entrada (opcional).
        /// </summary>
        public string? Note { get; set; }
    }
}
