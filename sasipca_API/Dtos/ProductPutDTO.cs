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
        /// Identificador da categoria do produto.
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// Identificador do tipo de unidade do produto.
        /// </summary>
        public int? UnitId { get; set; }

        /// <summary>
        /// (opcional) Número de DIAS com que manda uma notificação a avisar
        /// os utilizadores de algum grupo deste produto a expirar.
        /// </summary>
        public int? ExpNotif { get; set; }
    }
}
