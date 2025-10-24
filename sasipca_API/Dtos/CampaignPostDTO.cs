using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// Payload para criação de uma nova Campanha, suportando upload de imagem.
    /// Deve ser enviado como form-data.
    /// </summary>
    public class CampaignPostDTO
    {
        [Required(ErrorMessage = "O nome da campanha é obrigatório.")]
        [MaxLength(255)]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "A data de início é obrigatória.")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "A data de fim é obrigatória.")]
        public DateTime EndDate { get; set; }

        public string? Description { get; set; }

        public string? Location { get; set; }

        /// <summary>
        /// O ficheiro da imagem a ser guardada no servidor (enviado como IFormFile).
        /// </summary>
        public IFormFile? ImageFile { get; set; }
    }
}
