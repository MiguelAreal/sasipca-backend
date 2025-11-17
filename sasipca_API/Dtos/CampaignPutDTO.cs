using System.ComponentModel.DataAnnotations;

namespace sasipca_API.Dtos
{
    /// <summary>
    /// Payload para atualização de uma Campanha.
    /// </summary>
    public class CampaignPutDTO
    {
        [Required(ErrorMessage = "O nome da campanha é obrigatório.")]
        [MaxLength(255)]
        public string Name { get; set; } = null!;

        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public string? Description { get; set; }

        public string? Location { get; set; }

        /// <summary>
        /// Novo ficheiro de imagem para substituição.
        /// </summary>
        public IFormFile? NewImageFile { get; set; }

        /// <summary>
        /// Se verdadeiro, remove a imagem existente sem substituí-la.
        /// </summary>
        public bool RemoveImage { get; set; } = false;
    }
}
