namespace sasipca_API.Dtos
{

    /// <summary>
    /// Objeto DTO de modalidade de preço.
    /// </summary>
    public class ModalidadeGetDTO
    {
        /// <summary>
        /// Identificador da modalidade de preço.
        /// </summary>
        public int IdModalidade { get; set; }

        /// <summary>
        /// Nome da modalidade de preço.
        /// </summary>
        public string Nome { get; set; } = null!;
    }
}
