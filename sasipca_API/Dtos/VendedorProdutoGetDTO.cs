namespace sasipca_API.Dtos
{

    /// <summary>
    /// Objeto de Vendedor (Pessoa) de um produto.
    /// </summary>
    public class VendedorProdutoGetDTO
    {
        /// <summary>
        /// Identificador do vendedor do produto.
        /// </summary>
        public int IdVendedor { get; set; }

        /// <summary>
        /// Nome do vendedor do produto.
        /// </summary>
        public string Nome { get; set; } = null!;
    }
}
