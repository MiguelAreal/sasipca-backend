using System.ComponentModel.DataAnnotations;
using Xunit.Sdk;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// Item da lista de produtos/grupos buscados.
    /// </summary>
    public class DeliveryItemGetDTO
    {
        /// <summary>
        /// Nome do produto que entregue.
        /// </summary>
        public string Name { get; set; } = null!;

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
