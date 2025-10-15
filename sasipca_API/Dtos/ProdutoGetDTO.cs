using sasipca_API.Models;

namespace sasipca_API.Dtos
{

    /// <summary>
    /// Utilizado para ir buscar produtos.
    /// </summary>
    public class ProdutoGetDTO : ProdutoDTO
    {
        /// <summary>
        /// Identificador do produto.
        /// </summary>
        public int IdProduto { get; set; }


        /// <summary>
        /// Identifica se o utilizador que fez o pedido é ou não o vendedor do produto.
        /// </summary>
        public bool IsVendedor { get; set; }

        /// <summary>
        /// Identificador do estado em que o produto se encontra.
        /// </summary>
        public int? IdEstado { get; set; }

        /// <summary>
        /// Data de criação do produto.
        /// </summary>
        public DateTime DataCriacao { get; set; }

        /// <summary>
        /// Objeto de Vendedor (Pessoa) de um produto.
        /// </summary>
        public PessoaSimpleDTO Vendedor { get; set; } = null!;

        /// <summary>
        /// Objeto que contém o identificador e nome da categoria do produto.
        /// </summary>
        public CategoriaProdutoGetDTO Categoria { get; set; } = null!;

        /// <summary>
        /// Lista que contém os URLS das imagens do produto.
        /// </summary>
        public List<string> Imagens { get; set; } = new List<string>();
    }
}
