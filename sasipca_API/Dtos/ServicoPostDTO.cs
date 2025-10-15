using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{

    /// <summary>
    /// DTO utilizado para registar um novo Serviço.
    /// </summary>
    public class ServicoPostDTO : ServicoDTO
    {
        /// <summary>
        /// ID da Modalidade de Preço do Serviço.
        /// </summary>
        [Required]
        public int IdModalidadePreco { get; set; }

        /// <summary>
        /// Lista de imagens associadas ao Serviço (máximo de 6 imagens).
        /// </summary>
        public List<IFormFile>? Imagens { get; set; }
    }
}
