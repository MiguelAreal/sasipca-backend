using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// Classe base de Data Transfer Object para lista de produtos.
    /// </summary>
    public class ProductListDTO
    {
        /// <summary>
        /// Identificador do Produto
        /// </summary>
        public string Barcode { get; set; }

        /// <summary>
        /// Nome do Produto
        /// </summary>
        [Required]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Tipo de Unidade do produto.
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// Quantidade que um item tráz ("1" kg, "2" Litros)
        /// </summary>
        public int? UnitSize { get; set; }

        /// <summary>
        /// Tipo de Categoria do Produto.
        /// </summary>
        public string Category { get; set; } = null!;

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


    }
}
