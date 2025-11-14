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
        public int? UnitSize { get; set; }

        /// <summary>
        /// Objeto que contém o identificador da categoria do produto.
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// Objeto que contém o identificador do tipo de unidade do produto.
        /// </summary>
        public int UnitId { get; set; }
       
        /// <summary>
        /// Quantidade total real/tangível do produto (De todos os lotes combinado)
        /// </summary>
        public int? TotalQuantity { get; set; }

        /// <summary>
        /// Quantidade total reservada do produto (De todas as entregas planeadas)
        /// </summary>
        public int? ReservedQuantity { get; set; }

        /// <summary>
        /// Quantidade total disponível do produto (Quantidade Total - Quantidade Reservada)
        /// </summary>
        public int? AvailableStock { get; set; }

        public List<ProductLotDTO> ProductLots { get; set; } = new List<ProductLotDTO>();

    }
}
