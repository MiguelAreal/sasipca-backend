namespace sasipca_API.Dtos
{

    /// <summary>
    /// Objeto de categoria de um produto.
    /// </summary>
    public class ProductCategoryGetDTO
    {
        /// <summary>
        /// Identificador da categoria do produto.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome da categoria do produto.
        /// </summary>
        public string Type { get; set; } = null!;
    }
}
