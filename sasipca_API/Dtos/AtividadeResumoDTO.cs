namespace sasipca_API.Dtos
{
    namespace sasipca_API.Dtos
    {
        /// <summary>
        /// DTO para resumo de atividades
        /// </summary>
        public class AtividadeResumoDTO
        {
            /// <summary>
            /// ID da atividade
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// Nome da atividade
            /// </summary>
            public string Nome { get; set; }

            /// <summary>
            /// Tipo de atividade (PropostaProduto, PropostaServico, InscricaoEvento)
            /// </summary>
            public string Tipo { get; set; }

            /// <summary>
            /// Estado atual da atividade
            /// </summary>
            public string Estado { get; set; }

            /// <summary>
            /// Data de criação da atividade
            /// </summary>
            public DateTime DataCriacao { get; set; }

            /// <summary>
            /// ID do anúncio original (produto/serviço/evento)
            /// </summary>
            public int IdOriginal { get; set; }
        }
    }
}
