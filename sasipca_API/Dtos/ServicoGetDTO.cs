namespace sasipca_API.Dtos
{

    /// <summary>
    /// DTO utilizado para obter os detalhes de um Serviço.
    /// </summary>
    public class ServicoGetDTO : ServicoDTO
    {
        /// <summary>
        /// Identificador do Serviço.
        /// </summary>
        public int IdServico { get; set; }

        /// <summary>
        /// Identifica se o utilizador que fez o pedido é ou não o criador.
        /// </summary>
        public bool IsCriador { get; set; }

        /// <summary>
        /// Identificador do estado em que o serviço se encontra.
        /// </summary>
        public int? IdEstado { get; set; }

        /// <summary>
        /// Informações sobre o Criador do Serviço.
        /// </summary>
        public PessoaSimpleDTO Criador { get; set; } = null!;

        /// <summary>
        /// Modalidade de Preço do Serviço (por hora ou total).
        /// </summary>
        public string ModalidadePreco { get; set; } = null!;

        /// <summary>
        /// Data e Hora de criação do Serviço.
        /// </summary>
        public DateTime DataCriacao { get; set; }

        /// <summary>
        /// Lista de URLs das imagens associadas ao Serviço.
        /// </summary>
        public List<string> Imagens { get; set; } = new List<string>();
    }

}
