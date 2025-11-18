using sasipca_API.DBModels;
using sasipca_API.Models;

namespace sasipca_API.Dtos
{

    /// <summary>
    /// Utilizado para atualizar cabeçalho de produto.
    /// </summary>
    public class ProductPutDTO
    {
        /// <summary>
        /// Nome do produto
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Quantidade do produto (quanto vem num pacote)
        /// </summary>
        public int? UnitSize { get; set; }

        /// <summary>
        /// Objeto que contém o identificador da categoria do produto.
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// Objeto que contém o identificador do tipo de unidade do produto.
        /// </summary>
        public int? UnitId { get; set; }
    }
}
