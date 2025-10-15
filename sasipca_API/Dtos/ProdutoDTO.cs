using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{

    /// <summary>
    /// Classe base de Data Transfer Object para produtos.
    /// </summary>
    public class ProdutoDTO
    {
        /// <summary>
        /// Nome/Título do Produto.
        /// </summary>
        public string Nome { get; set; } = null!;

        /// <summary>
        /// Preço do Produto.
        /// </summary>
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
        public decimal Preco { get; set; }

        /// <summary>
        /// Descrição do Produto
        /// </summary>
        public string? Descricao { get; set; }

    }
}
