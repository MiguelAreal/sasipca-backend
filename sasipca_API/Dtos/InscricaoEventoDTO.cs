namespace sasipca_API.Dtos
{
    public class InscricaoEventoDTO
    {
        /// <summary>
        /// ID da inscrição
        /// </summary>
        public int IdInscricao { get; set; }

        /// <summary>
        /// ID da pessoa inscrita
        /// </summary>
        public int IdPessoa { get; set; }

        /// <summary>
        /// Nome da pessoa inscrita
        /// </summary>
        public string NomePessoa { get; set; }

        /// <summary>
        /// Data da inscrição
        /// </summary>
        public DateTime DataInscricao { get; set; }

        /// <summary>
        /// Item selecionado para levar (pode ser nulo se não houver itens ou se todos já estiverem selecionados)
        /// </summary>
        public ItemInscricaoDTO Item { get; set; }
    }
}
