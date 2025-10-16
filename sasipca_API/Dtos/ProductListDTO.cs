using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// Classe base de Data Transfer Object para lista de anúncios (Produtos,eventos e serviços).
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
        /// Tipo de Categoria do Produto.
        /// </summary>
        [Required]
        public string Category { get; set; } = null!;

    }
}
