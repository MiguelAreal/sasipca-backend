using sasipca_API.DBModels;
using sasipca_API.Models;

namespace sasipca_API.Dtos
{

    /// <summary>
    /// Utilizado para ir buscar produtos.
    /// </summary>
    public class ProductGetDTO
    {
        /// <summary>
        /// Identificador do produto.
        /// </summary>
        public string Barcode { get; set; }

        /// <summary>
        /// Nome do produto
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Quantidade do produto (quanto vem num pacote)
        /// </summary>
        public int? Quantity { get; set; }

        /// <summary>
        /// Objeto que contém o identificador e nome da categoria do produto.
        /// </summary>
        public string Category { get; set; } = null!;

        /// <summary>
        /// Objeto que contém o identificador e nome da categoria do produto.
        /// </summary>
        public string Unit { get; set; } = null!;

        /// <summary>
        /// Lista dos lotes associados ao produto.
        /// </summary>
        public ICollection<ProductLotDTO> ProductLots { get; set; } = new List<ProductLotDTO>();

    }
}
