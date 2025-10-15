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
        public CategoryType Category { get; set; } = null!;

        /// <summary>
        /// Objeto que contém o identificador e nome da categoria do produto.
        /// </summary>
        public UnitType Unit { get; set; } = null!;

    }
}
