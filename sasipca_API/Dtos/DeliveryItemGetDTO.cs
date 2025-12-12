using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// Item da lista de produtos/grupos buscados.
    /// </summary>
    public class DeliveryItemGetDTO
    {
        /// <summary>
        /// Nome do produto entregue.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Código de Barras do produto (para edição).
        /// </summary>
        public string? Barcode { get; set; }

        /// <summary>
        /// ID do Grupo/Lote (para edição).
        /// </summary>
        public int? GroupId { get; set; }

        /// <summary>
        /// Validade do grupo específico
        /// </summary>
        public DateOnly ExpiryDate { get; set; }

        /// <summary>
        /// Quantidade movimentada.
        /// </summary>
        public int Quantity { get; set; }
    }
}