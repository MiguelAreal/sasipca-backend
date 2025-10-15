namespace sasipca_API.Dtos
{

    /// <summary>
    /// Utilizado para registar produtos.
    /// </summary>
    public class ProdutoPostDTO : ProdutoDTO
    {
        /// <summary>
        /// Especifica a categoria do produto pelo ID.
        /// </summary>
        public int IdCategoria { get; set; }

        /// <summary>
        /// Lista de imagens enviadas pelo utilizador (máx. 4).
        /// </summary>
        public List<IFormFile>? Imagens { get; set; }
    };
}
