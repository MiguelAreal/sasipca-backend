using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{
    public class ProductReceiptDTO
    {
        /// <summary>
        /// Código de barras do produto (obrigatório).
        /// </summary>
        [Required(ErrorMessage = "O Barcode é obrigatório.")]
        public string Barcode { get; set; } = null!;

        /// <summary>
        /// Nome do produto (obrigatório se o produto for novo).
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// ID da Categoria (obrigatório se o produto for novo).
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// ID da Unidade (obrigatório se o produto for novo).
        /// </summary>
        public int? UnitId { get; set; }

        /// <summary>
        /// Quantidade por unidade (ex: 1kg).
        /// </summary>
        public int? UnitSize { get; set; }
    }
}
