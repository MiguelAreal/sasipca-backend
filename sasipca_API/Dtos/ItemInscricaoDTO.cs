namespace sasipca_API.Dtos
{
    public class ItemInscricaoDTO
    {
        /// <summary>
        /// ID do item
        /// </summary>
        public int? IdItem { get; set; }

        /// <summary>
        /// Nome do item
        /// </summary>
        public string Nome { get; set; }

        /// <summary>
        /// Quantidade do item
        /// </summary>
        public int Quantidade { get; set; }
    }
}
