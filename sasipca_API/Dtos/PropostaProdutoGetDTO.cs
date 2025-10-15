namespace sasipca_API.Dtos
{

    /// <summary>
    /// DTO usado para buscar propostas.
    /// </summary>
    public class PropostaProdutoGetDTO
    {
        /// <summary>
        /// Identificação da Proposta a um Produto.
        /// </summary>
        public int IdPropostaProduto { get; set; }

        /// <summary>
        /// Valor dado pelo comprador.
        /// </summary>
        public decimal Valor { get; set; }

        /// <summary>
        /// Dados do comprador.
        /// </summary>
        public PessoaSimpleDTO Comprador { get; set; } = null!;

    }
}
