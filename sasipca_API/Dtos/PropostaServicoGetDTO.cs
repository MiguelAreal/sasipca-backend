namespace sasipca_API.Dtos
{

    /// <summary>
    /// DTO usado para buscar propostas de um serviço.
    /// </summary>
    public class PropostaServicoGetDTO
    {
        /// <summary>
        /// Identificação da Proposta a um Serviço.
        /// </summary>
        public int IdPropostaServico { get; set; }

        /// <summary>
        /// Dados do executor do serviço.
        /// </summary>
        public PessoaSimpleDTO Executor { get; set; } = null!;

    }
}
