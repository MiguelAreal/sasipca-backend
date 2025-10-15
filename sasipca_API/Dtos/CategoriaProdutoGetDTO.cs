namespace sasipca_API.Dtos
{

    /// <summary>
    /// Objeto de categoria de um produto.
    /// </summary>
    public class CategoriaProdutoGetDTO
    {
        /// <summary>
        /// Identificador da categoria do produto.
        /// </summary>
        public int IdCategoria { get; set; }

        /// <summary>
        /// Nome da categoria do produto.
        /// </summary>
        public string Nome { get; set; } = null!;
    }
}
